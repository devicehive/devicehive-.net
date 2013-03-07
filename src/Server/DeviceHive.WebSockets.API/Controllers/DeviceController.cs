using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.MessageLogic;
using DeviceHive.Core.Messaging;
using DeviceHive.Core.Services;
using DeviceHive.Data;
using DeviceHive.Data.Model;
using DeviceHive.WebSockets.API.Subscriptions;
using DeviceHive.WebSockets.Core.ActionsFramework;
using DeviceHive.WebSockets.Core.Network;
using Newtonsoft.Json.Linq;
using Ninject;

namespace DeviceHive.WebSockets.API.Controllers
{
    /// <summary>
    /// <para>
    /// The service allows devices to exchange messages with the DeviceHive server using a single persistent connection.
    /// </para>
    /// <para>
    /// After connection is eshtablished, devices need to register using the device/save message,
    /// perform authentication and then start sending notifications using the notification/insert message.
    /// </para>
    /// <para>
    /// Devices may also subscribe to commands using the command/subscrive message
    /// and then start receiving server-originated messages about new commands.
    /// </para>
    /// <para>
    /// It is also possible not to authenticate as concrete device, but rather send device identifier and key in each request.
    /// That scenario is common for gateways which typically proxy multiple devices and use a single connection to the server.
    /// </para>
    /// </summary>
    public class DeviceController : ControllerBase
    {
        #region Private Fields

        private readonly DeviceSubscriptionManager _subscriptionManager;
        private readonly MessageBus _messageBus;
        private readonly IMessageManager _messageManager;
        private readonly DeviceService _deviceService;

        private readonly IJsonMapper<Device> _deviceMapper;

        #endregion

        #region Constructor

        public DeviceController(ActionInvoker actionInvoker,
            DataContext dataContext, JsonMapperManager jsonMapperManager,
            [Named("DeviceCommand")] DeviceSubscriptionManager subscriptionManager,
            MessageBus messageBus, IMessageManager messageManager,
            DeviceService deviceService) :
            base(actionInvoker, dataContext, jsonMapperManager)
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

        #region ControllerBase Members

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

        #region Actions Methods

        /// <summary>
        /// Authenticates a device.
        /// After successful authentication, all subsequent messages may exclude deviceId and deviceKey parameters.
        /// </summary>
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

        /// <summary>
        /// Creates new device notification.
        /// </summary>
        /// <param name="notification" cref="DeviceNotification">A <see cref="DeviceNotification"/> resource to create.</param>
        /// <response>
        ///     <parameter name="notification" cref="DeviceNotification">An inserted <see cref="DeviceNotification"/> resource.</parameter>
        /// </response>
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

        /// <summary>
        /// Updates an existing device command.
        /// </summary>
        /// <param name="commandId">Device command identifier.</param>
        /// <param name="command" cref="DeviceCommand">A <see cref="DeviceCommand"/> resource to update.</param>
        /// <request>
        ///     <parameter name="command.command" required="false" />
        /// </request>
        /// <response>
        ///     <parameter name="command" cref="DeviceCommand">An updated <see cref="DeviceCommand"/> resource.</parameter>
        /// </response>
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

        /// <summary>
        /// Subscribes the device to commands.
        /// After subscription is completed, the server will start to send command/insert messages to the connected device.
        /// </summary>
        /// <param name="timestamp">Timestamp of the last received command (UTC). If not specified, the server's timestamp is taken instead.</param>
        [Action("command/subscribe", NeedAuthentication = true)]
        public void SubsrcibeToDeviceCommands(DateTime? timestamp)
        {
            if (timestamp != null)
            {
                var initialCommandList = GetInitialCommandList(Connection);

                lock (initialCommandList)
                {
                    var filter = new DeviceCommandFilter { Start = timestamp.Value.AddTicks(10) };
                    var initialCommands = DataContext.DeviceCommand.GetByDevice(
                        CurrentDevice.ID, filter);

                    foreach (var command in initialCommands)
                    {
                        initialCommandList.Add(command.ID);
                        Notify(Connection, CurrentDevice, command, isInitialCommand: true);
                    }
                }
            }
            
            _subscriptionManager.Subscribe(Connection, CurrentDevice.ID);
            SendSuccessResponse();
        }

        /// <summary>
        /// Unsubscribes the device from commands.
        /// </summary>
        [Action("command/unsubscribe", NeedAuthentication = true)]
        public void UnsubsrcibeFromDeviceCommands()
        {
            _subscriptionManager.Unsubscribe(Connection, CurrentDevice.ID); 
            SendSuccessResponse();
        }

        /// <summary>
        /// Gets information about the current device.
        /// </summary>
        /// <response>
        ///     <parameter name="device" cref="Device">The <see cref="Device"/> resource representing the current device.</parameter>
        /// </response>
        [Action("device/get", NeedAuthentication = true)]
        public void GetDevice()
        {
            var device = DataContext.Device.Get(CurrentDevice.ID);
            SendResponse(new JProperty("device", _deviceMapper.Map(device)));
        }

        /// <summary>
        /// Registers or updates a device.
        /// A valid device key is required in the deviceKey parameter in order to update an existing device.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="device" cref="Device">A <see cref="Device"/> resource to register or update.</param>
        /// <request>
        ///     <parameter name="device.network" mode="remove" />
        ///     <parameter name="device.deviceClass" mode="remove" />
        ///     <parameter name="device.network" type="integer or object" required="false">
        ///         <para>Network identifier or <see cref="Network"/> object.</para>
        ///         <para>If object is passed, the target network will be searched by name and automatically created if not found.</para>
        ///         <para>In case when existing network is protected with the key, the key value must be included.</para>
        ///     </parameter>
        ///     <parameter name="device.deviceClass" type="integer or object" required="true">
        ///         <para>Device class identifier or <see cref="DeviceClass"/> object.</para>
        ///         <para>If object is passed, the target device class will be searched by name and version, and automatically created if not found.</para>
        ///         <para>The device class object will be also updated accordingly unless the DeviceClass.IsPermanent flag is set.</para>
        ///     </parameter>
        ///     <parameter name="device.equipment" type="array" required="false" cref="Equipment">
        ///         <para>Array of <see cref="Equipment"/> objects to be associated with the device class. If specified, all existing values will be replaced.</para>
        ///         <para>In case when device class is permanent, this value is ignored.</para>
        ///     </parameter>
        /// </request>
        /// <response>
        ///     <parameter name="device" cref="Device">The <see cref="Device"/> resource representing the registered/updated device.</parameter>
        /// </response>
        [Action("device/save", NeedAuthentication = false)]
        public void SaveDevice(Guid deviceId, JObject device)
        {
            try
            {
                // load device from repository
                var deviceEntity = DataContext.Device.Get(deviceId);
                if (deviceEntity != null)
                {
                    if (CurrentDevice == null)
                    {
                        if (!AuthenticateImpl())
                            throw new WebSocketRequestException("Not authorized");
                    }

                    if (CurrentDevice.GUID != deviceId)
                        throw new WebSocketRequestException("Not authorized");
                }
                else
                {
                    // otherwise, create new device
                    deviceEntity = new Device(deviceId);
                }

                device = _deviceService.SaveDevice(deviceEntity, device);
                SendResponse(new JProperty("device", device));
            }
            catch (ServiceException e)
            {
                SendErrorResponse(e.Message);
            }            
        }

        #endregion

        #region Notification Handling

        public void HandleDeviceCommand(int deviceId, int commandId)
        {
            var connections = _subscriptionManager.GetConnections(deviceId);
            if (connections.Any())
            {
                var command = DataContext.DeviceCommand.Get(commandId);
                var device = DataContext.Device.Get(deviceId);

                foreach (var connection in connections)
                    Notify(connection, device, command);
            }
        }

        private void CleanupNotifications(WebSocketConnectionBase connection)
        {
            _subscriptionManager.Cleanup(connection);
        }

        /// <summary>
        /// Notifies the device about new command.
        /// </summary>
        /// <action>command/insert</action>
        /// <response>
        ///     <parameter name="deviceGuid" type="guid">Device unique identifier.</parameter>
        ///     <parameter name="command" cref="DeviceCommand">A <see cref="DeviceCommand"/> resource representing the command.</parameter>
        /// </response>
        private void Notify(WebSocketConnectionBase connection, Device device, DeviceCommand command,
            bool isInitialCommand = false)
        {
            if (!isInitialCommand)
            {
                var initialCommandList = GetInitialCommandList(connection);
                lock (initialCommandList)
                {
                    if (initialCommandList.Contains(command.ID))
                        return;
                }
            }

            connection.SendResponse("command/insert",
                new JProperty("deviceGuid", device.GUID),
                new JProperty("command", CommandMapper.Map(command)));
        }

        #endregion

        #region Private Methods

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

        private ISet<int> GetInitialCommandList(WebSocketConnectionBase connection)
        {
            return (ISet<int>) connection.Session.GetOrAdd("InitialCommands", () => new HashSet<int>());
        }

        #endregion
    }
}