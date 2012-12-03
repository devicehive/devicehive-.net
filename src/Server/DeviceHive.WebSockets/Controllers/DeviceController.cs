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

        private readonly SubscriptionManager _subscriptionManager;
        private readonly MessageBus _messageBus;
        private readonly IMessageManager _messageManager;

        #endregion

        #region Constructor

        public DeviceController(ActionInvoker actionInvoker, WebSocketServerBase server,
            DataContext dataContext, JsonMapperManager jsonMapperManager,
            [Named("DeviceCommand")] SubscriptionManager subscriptionManager,
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
            get { return (Device) Connection.Session["device"]; }
            set { Connection.Session["device"] = value; }
        }

        #endregion

        #region Methods

        #region Overrides

        public override bool IsAuthenticated
        {
            get { return CurrentDevice != null; }
        }

        #endregion

        #region Actions

        [Action("authenticate")]
        public void Authenticate()
        {
            var deviceId = Guid.Parse((string) ActionArgs["deviceId"]);
            var deviceKey = (string) ActionArgs["deviceKey"];

            var device = DataContext.Device.Get(deviceId);
            if (device == null || device.Key != deviceKey)
                throw new WebSocketRequestException("Device not found");

            CurrentDevice = device;
            SendSuccessResponse();
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
            _messageBus.Notify(new DeviceNotificationAddedMessage(CurrentDevice.GUID, notification.ID));

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

            commandObj = CommandMapper.Map(command);
            SendResponse(new JProperty("command", commandObj));
        }

        [Action("command/subscribe", NeedAuthentication = true)]
        public void SubsrcibeToDeviceCommands()
        {            
            _subscriptionManager.Subscribe(Connection, CurrentDevice.GUID);
            SendSuccessResponse();
        }

        [Action("command/unsubscribe", NeedAuthentication = true)]
        public void UnsubsrcibeFromDeviceCommands()
        {
            _subscriptionManager.Unsubscribe(Connection, CurrentDevice.GUID); 
            SendSuccessResponse();
        }

        #endregion

        #region Notification handling

        public void HandleDeviceCommand(Guid deviceGuid, int commandId)
        {
            var command = DataContext.DeviceCommand.Get(commandId);
            var connections = _subscriptionManager.GetConnections(deviceGuid);

            foreach (var connection in connections)
                Notify(connection, command);
        }

        public void CleanupNotifications(WebSocketConnectionBase connection)
        {
            _subscriptionManager.Cleanup(connection);
        }

        private void Notify(WebSocketConnectionBase connection, DeviceCommand command)
        {
            var device = (Device) connection.Session["device"];
            if (device == null || device.ID != command.DeviceID)
                return;

            SendResponse(connection, "command/notify",
                new JProperty("command", CommandMapper.Map(command)));
        }

        #endregion

        #endregion
    }
}