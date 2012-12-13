using System;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.MessageLogic;
using DeviceHive.Core.Messaging;
using DeviceHive.Core.Services;
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
        private readonly DeviceService _deviceService;

        private readonly IJsonMapper<Device> _deviceMapper;

        #endregion

        #region Constructor

        public DeviceController(ActionInvoker actionInvoker, WebSocketServerBase server,
            DataContext dataContext, JsonMapperManager jsonMapperManager,
            [Named("DeviceCommand")] DeviceSubscriptionManager subscriptionManager,
            MessageBus messageBus, IMessageManager messageManager,
            DeviceService deviceService) :
            base(actionInvoker, server, dataContext, jsonMapperManager)
        {
            _subscriptionManager = subscriptionManager;
            _messageBus = messageBus;
            _messageManager = messageManager;
            _deviceService = deviceService;

            _deviceMapper = jsonMapperManager.GetMapper<Device>();
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
        public void InsertDeviceNotification(JObject notification)
        {
            if (notification == null)
                throw new WebSocketRequestException("Please specify notification");

            var notificationEntity = NotificationMapper.Map(notification);
            notificationEntity.Device = CurrentDevice;
            Validate(notificationEntity);

            DataContext.DeviceNotification.Save(notificationEntity);
            _messageManager.ProcessNotification(notificationEntity);
            _messageBus.Notify(new DeviceNotificationAddedMessage(CurrentDevice.ID, notificationEntity.ID));

            notification = NotificationMapper.Map(notificationEntity);
            SendResponse(new JProperty("notification", notification));
        }

        [Action("command/update", NeedAuthentication = true)]
        public void UpdateDeviceCommand(int commandId, JObject command)
        {
            if (commandId == 0)
                throw new WebSocketRequestException("Please specify valid commandId");

            if (command == null)
                throw new WebSocketRequestException("Please specify command");

            var commandEntity = DataContext.DeviceCommand.Get(commandId);
            if (commandEntity == null || commandEntity.DeviceID != CurrentDevice.ID)
                throw new WebSocketRequestException("Device command not found");

            CommandMapper.Apply(commandEntity, command);
            commandEntity.Device = CurrentDevice;
            Validate(commandEntity);

            DataContext.DeviceCommand.Save(commandEntity);
            _messageBus.Notify(new DeviceCommandUpdatedMessage(CurrentDevice.ID, commandEntity.ID));

            command = CommandMapper.Map(commandEntity);
            SendResponse(new JProperty("command", command));
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

        [Action("device/get", NeedAuthentication = true)]
        public void GetDevice()
        {
            SendResponse(new JProperty("device", _deviceMapper.Map(CurrentDevice)));
        }

        [Action("device/save", NeedAuthentication = false)]
        public void SaveDevice(Guid deviceGuid, JObject device)
        {
            try
            {
                // load device from repository
                var deviceEntity = DataContext.Device.Get(deviceGuid);
                if (deviceEntity != null)
                {
                    if (!AuthenticateImpl() || CurrentDevice.GUID != deviceGuid)
                        throw new WebSocketRequestException("Not authorized");
                }
                else
                {
                    // otherwise, create new device
                    deviceEntity = new Device(deviceGuid);
                }

                AuthenticateImpl();

                device = _deviceService.SaveDevice(deviceEntity, device);
                SendResponse(new JProperty("device", device));
            }
            catch (ServiceException e)
            {
                SendErrorResponse(e.Message);
            }            
        }

        #endregion

        #region Notification handling

        public void HandleDeviceCommand(int deviceId, int commandId)
        {
            var command = DataContext.DeviceCommand.Get(commandId);
            var device = DataContext.Device.Get(deviceId);
            var connections = _subscriptionManager.GetConnections(deviceId);

            foreach (var connection in connections)
                Notify(connection, device, command);
        }

        private void CleanupNotifications(WebSocketConnectionBase connection)
        {
            _subscriptionManager.Cleanup(connection);
        }

        private void Notify(WebSocketConnectionBase connection, Device device, DeviceCommand command)
        {
            connection.SendResponse("command/insert",
                new JProperty("deviceGuid", device.GUID),
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