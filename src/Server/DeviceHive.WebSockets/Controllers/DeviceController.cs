using System;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.MessageLogic;
using DeviceHive.Core.Messaging;
using DeviceHive.Data;
using DeviceHive.Data.Model;
using DeviceHive.WebSockets.ActionsFramework;
using DeviceHive.WebSockets.Network;
using DeviceHive.WebSockets.Subscriptions;
using Newtonsoft.Json.Linq;
using Ninject;

namespace DeviceHive.WebSockets.Controllers
{
    public class DeviceController : ControllerBase
    {
        #region Private fields

        private readonly DeviceSubscriptionManager _subscriptionManager;
        private readonly MessageBus _messageBus;
        private readonly IMessageManager _messageManager;

        #endregion

        #region Constructor

        public DeviceController(ActionInvoker actionInvoker, WebSocketServerBase server,
            DataContext dataContext, JsonMapperManager jsonMapperManager,
            [Named("DeviceCommand")] DeviceSubscriptionManager subscriptionManager,
            MessageBus messageBus, IMessageManager messageManager) :
            base(actionInvoker, server, dataContext, jsonMapperManager)
        {
            _subscriptionManager = subscriptionManager;
            _messageBus = messageBus;
            _messageManager = messageManager;
        }

        #endregion

        #region Properties

        private Device CurrentDevice
        {
            get { return RequestDevice ?? SessionDevice; }
        }

        private Device SessionDevice
        {
            get { return (Device)Connection.Session["device"]; }
            set { Connection.Session["device"] = value; }
        }

        private Device RequestDevice { get; set; }

        #endregion

        #region Overrides

        public override bool IsAuthenticated
        {
            get
            {
                AuthenticateImpl();
                return CurrentDevice != null;
            }
        }

        protected override void BeforeActionInvoke()
        {
            RequestDevice = null;
        }

        public override void CleanupConnection(WebSocketConnectionBase connection)
        {
            base.CleanupConnection(connection);
            CleanupNotifications(connection);
        }

        #endregion

        #region Methods

        #region Actions

        [Action("authenticate")]
        public void Authenticate()
        {
            if (AuthenticateImpl())
            {
                SessionDevice = RequestDevice;
                SendSuccessResponse();
                return;
            }

            throw new WebSocketRequestException("Please specify valid authentication data");
        }

        [Action("notification/insert", NeedAuthentication = true)]
        public void InsertDeviceNotification()
        {
            var notificationObj = (JObject) ActionArgs["notification"];
            
            var notification = NotificationMapper.Map(notificationObj);
            notification.Device = CurrentDevice;
            Validate(notification);

            DataContext.DeviceNotification.Save(notification);
            _messageManager.ProcessNotification(notification);
            _messageBus.Notify(new DeviceNotificationAddedMessage(CurrentDevice.ID, notification.ID));

            notificationObj = NotificationMapper.Map(notification);
            SendResponse(new JProperty("notification", notificationObj));
        }

        [Action("command/update", NeedAuthentication = true)]
        public void UpdateDeviceCommand()
        {
            var commandId = (int)ActionArgs["commandId"];
            var commandObj = (JObject)ActionArgs["command"];

            var command = DataContext.DeviceCommand.Get(commandId);
            if (command == null || command.DeviceID != CurrentDevice.ID)
                throw new WebSocketRequestException("Device command not found");

            CommandMapper.Apply(command, commandObj);
            command.Device = CurrentDevice;
            Validate(command);

            DataContext.DeviceCommand.Save(command);
            _messageBus.Notify(new DeviceCommandUpdatedMessage(CurrentDevice.ID, command.ID));

            commandObj = CommandMapper.Map(command);
            SendResponse(new JProperty("command", commandObj));
        }

        [Action("command/subscribe", NeedAuthentication = true)]
        public void SubsrcibeToDeviceCommands()
        {
            _subscriptionManager.Subscribe(Connection, CurrentDevice.ID);
            SendSuccessResponse();
        }

        [Action("command/unsubscribe", NeedAuthentication = true)]
        public void UnsubsrcibeFromDeviceCommands()
        {
            _subscriptionManager.Unsubscribe(Connection, CurrentDevice.ID); 
            SendSuccessResponse();
        }

        #endregion

        #region Notification handling

        public void HandleDeviceCommand(int deviceId, int commandId)
        {
            var command = DataContext.DeviceCommand.Get(commandId);
            var connections = _subscriptionManager.GetConnections(deviceId);

            foreach (var connection in connections)
                Notify(connection, command);
        }

        private void CleanupNotifications(WebSocketConnectionBase connection)
        {
            _subscriptionManager.Cleanup(connection);
        }

        private void Notify(WebSocketConnectionBase connection, DeviceCommand command)
        {
            connection.SendResponse("command/insert",
                new JProperty("command", CommandMapper.Map(command)));
        }

        #endregion

        #region Helper methods

        private bool AuthenticateImpl()
        {
            if (ActionArgs == null)
                return false;

            var deviceIdValue = ActionArgs["deviceId"];
            var deviceKeyValue = ActionArgs["deviceKey"];

            if (deviceIdValue == null || deviceKeyValue == null)
                return false;

            var deviceId = Guid.Parse((string)deviceIdValue);            
            var deviceKey = (string)deviceKeyValue;

            var device = DataContext.Device.Get(deviceId);
            if (device == null || device.Key != deviceKey)
                throw new WebSocketRequestException("Device not found");

            RequestDevice = device;
            return true;
        }

        #endregion

        #endregion
    }
}