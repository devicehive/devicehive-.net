using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using DeviceHive.Core;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.MessageLogic;
using DeviceHive.Core.Messaging;
using DeviceHive.Core.Services;
using DeviceHive.Data;
using DeviceHive.Data.Model;
using DeviceHive.WebSockets.API.Filters;
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

        private static readonly DeviceSubscriptionManager _subscriptionManager = new DeviceSubscriptionManager();

        private readonly MessageBus _messageBus;
        private readonly IMessageManager _messageManager;
        private readonly DeviceService _deviceService;

        #endregion

        #region Constructor

        public DeviceController(ActionInvoker actionInvoker, DataContext dataContext,
            JsonMapperManager jsonMapperManager, DeviceHiveConfiguration deviceHiveConfiguration,
            MessageBus messageBus, IMessageManager messageManager, DeviceService deviceService) :
            base(actionInvoker, dataContext, jsonMapperManager, deviceHiveConfiguration)
        {
            _messageBus = messageBus;
            _messageManager = messageManager;
            _deviceService = deviceService;
        }

        #endregion

        #region Properties

        private Device CurrentDevice
        {
            get { return (Device)ActionContext.GetParameter("AuthDevice") ?? SessionDevice; }
        }

        private Device SessionDevice
        {
            get { return (Device)Connection.Session["Device"]; }
            set { Connection.Session["Device"] = value; }
        }

        #endregion

        #region ControllerBase Members

        public override void CleanupConnection(WebSocketConnectionBase connection)
        {
            base.CleanupConnection(connection);
            _subscriptionManager.Cleanup(connection);
        }

        #endregion

        #region Actions Methods

        /// <summary>
        /// Authenticates a device.
        /// After successful authentication, all subsequent messages may exclude deviceId and deviceKey parameters.
        /// </summary>
        /// <request>
        ///     <parameter name="deviceId" type="guid" required="true">Device unique identifier.</parameter>
        ///     <parameter name="deviceKey" type="string" required="true">Device authentication key.</parameter>
        /// </request>
        [Action("authenticate")]
        [AuthenticateDevice]
        public void Authenticate()
        {
            var device = (Device)ActionContext.GetParameter("AuthDevice");
            if (device == null)
                throw new WebSocketRequestException("Please specify valid authentication data");

            SessionDevice = device;
            SendSuccessResponse();
        }

        /// <summary>
        /// Creates new device notification.
        /// </summary>
        /// <param name="notification" cref="DeviceNotification">A <see cref="DeviceNotification"/> resource to create.</param>
        /// <response>
        ///     <parameter name="notification" cref="DeviceNotification" mode="OneWayOnly">An inserted <see cref="DeviceNotification"/> resource.</parameter>
        /// </response>
        [Action("notification/insert")]
        [AuthenticateDevice, AuthorizeDevice]
        public void InsertDeviceNotification(JObject notification)
        {
            if (notification == null)
                throw new WebSocketRequestException("Please specify notification");

            var notificationEntity = GetMapper<DeviceNotification>().Map(notification);
            notificationEntity.Device = CurrentDevice;
            Validate(notificationEntity);

            DataContext.DeviceNotification.Save(notificationEntity);
            _messageManager.ProcessNotification(notificationEntity);
            _messageBus.Notify(new DeviceNotificationAddedMessage(CurrentDevice.ID, notificationEntity.ID));

            notification = GetMapper<DeviceNotification>().Map(notificationEntity, oneWayOnly: true);
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
        [Action("command/update")]
        [AuthenticateDevice, AuthorizeDevice]
        public void UpdateDeviceCommand(int commandId, JObject command)
        {
            if (commandId == 0)
                throw new WebSocketRequestException("Please specify valid commandId");

            if (command == null)
                throw new WebSocketRequestException("Please specify command");

            var commandEntity = DataContext.DeviceCommand.Get(commandId);
            if (commandEntity == null || commandEntity.DeviceID != CurrentDevice.ID)
                throw new WebSocketRequestException("Device command not found");

            GetMapper<DeviceCommand>().Apply(commandEntity, command);
            commandEntity.Device = CurrentDevice;
            Validate(commandEntity);

            DataContext.DeviceCommand.Save(commandEntity);
            _messageBus.Notify(new DeviceCommandUpdatedMessage(CurrentDevice.ID, commandEntity.ID));

            SendSuccessResponse();
        }

        /// <summary>
        /// Subscribes the device to commands.
        /// After subscription is completed, the server will start to send command/insert messages to the connected device.
        /// </summary>
        /// <param name="timestamp">Timestamp of the last received command (UTC). If not specified, the server's timestamp is taken instead.</param>
        [Action("command/subscribe")]
        [AuthenticateDevice, AuthorizeDevice]
        public void SubsrcibeToDeviceCommands(DateTime? timestamp)
        {
            var initialCommandList = GetInitialCommandList(Connection);
            lock (initialCommandList)
            {
                _subscriptionManager.Subscribe(Connection, CurrentDevice.ID);
                if (timestamp != null)
                {
                    var filter = new DeviceCommandFilter { Start = timestamp, IsDateInclusive = false };
                    var initialCommands = DataContext.DeviceCommand.GetByDevice(CurrentDevice.ID, filter);
                    foreach (var command in initialCommands)
                    {
                        initialCommandList.Add(command.ID);
                        Notify(Connection, CurrentDevice, command, isInitialCommand: true);
                    }
                }
            }
            
            SendSuccessResponse();
        }

        /// <summary>
        /// Unsubscribes the device from commands.
        /// </summary>
        [Action("command/unsubscribe")]
        [AuthenticateDevice, AuthorizeDevice]
        public void UnsubsrcibeFromDeviceCommands()
        {
            var subscriptions = _subscriptionManager.GetSubscriptions(Connection);
            foreach (var subscription in subscriptions)
            {
                if (subscription.Keys.Contains(CurrentDevice.ID))
                    _subscriptionManager.Unsubscribe(Connection, subscription.Id);
            }

            SendSuccessResponse();
        }

        /// <summary>
        /// Gets information about the current device.
        /// </summary>
        /// <response>
        ///     <parameter name="device" cref="Device">The <see cref="Device"/> resource representing the current device.</parameter>
        /// </response>
        [Action("device/get")]
        [AuthenticateDevice, AuthorizeDevice]
        public void GetDevice()
        {
            var device = DataContext.Device.Get(CurrentDevice.ID);
            SendResponse(new JProperty("device", GetMapper<Device>().Map(device)));
        }

        /// <summary>
        /// Registers or updates a device.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="device" cref="Device">A <see cref="Device"/> resource to register or update.</param>
        /// <request>
        ///     <parameter name="device.network">
        ///         <para>A <see cref="Network"/> object which includes name property to match.</para>
        ///         <para>In case when the target network is protected with a key, the key value must also be included.</para>
        ///         <para>For test deployments, any non-existing networks are automatically created.</para>
        ///     </parameter>
        ///     <parameter name="device.deviceClass">
        ///         <para>A <see cref="DeviceClass"/> object which includes name and version properties to match.</para>
        ///         <para>The device class objects are automatically created/updated unless the DeviceClass.IsPermanent flag is set.</para>
        ///     </parameter>
        /// </request>
        [Action("device/save")]
        [AuthorizeDeviceRegistration]
        public void SaveDevice(Guid deviceId, JObject device)
        {
            // get device as stored in the AuthorizeDeviceRegistration filter
            var deviceEntity = ActionContext.Parameters.ContainsKey("Device") ? (Device)ActionContext.Parameters["Device"] : new Device(deviceId);

            try
            {
                _deviceService.SaveDevice(deviceEntity, device);
                SendSuccessResponse();
            }
            catch (ServiceException e)
            {
                SendErrorResponse(e.Message);
            }
        }

        /// <summary>
        /// Gets meta-information of the current API.
        /// </summary>
        /// <response>
        ///     <parameter name="info" cref="ApiInfo">The <see cref="ApiInfo"/> resource.</parameter>
        /// </response>
        [Action("server/info")]
        public void ServerInfo()
        {
            var restEndpoint = DeviceHiveConfiguration.RestEndpoint;
            var apiInfo = new ApiInfo
            {
                ApiVersion = DeviceHive.Core.Version.ApiVersion,
                ServerTimestamp = DataContext.Timestamp.GetCurrentTimestamp(),
                RestServerUrl = restEndpoint.Uri,
            };

            SendResponse(new JProperty("info", GetMapper<ApiInfo>().Map(apiInfo)));
        }

        #endregion

        #region Notification Handling

        public void HandleDeviceCommand(int deviceId, int commandId)
        {
            var subscriptions = _subscriptionManager.GetSubscriptions(deviceId);
            if (subscriptions.Any())
            {
                var command = DataContext.DeviceCommand.Get(commandId);
                var device = DataContext.Device.Get(deviceId);

                foreach (var subscription in subscriptions)
                    Notify(subscription.Connection, device, command);
            }
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
                lock (initialCommandList) // wait until all initial commands are sent
                {
                    if (initialCommandList.Contains(command.ID))
                        return;
                }
            }

            connection.SendResponse("command/insert",
                new JProperty("deviceGuid", device.GUID),
                new JProperty("command", GetMapper<DeviceCommand>().Map(command)));
        }

        #endregion

        #region Private Methods

        private ISet<int> GetInitialCommandList(WebSocketConnectionBase connection)
        {
            return (ISet<int>) connection.Session.GetOrAdd("InitialCommands", () => new HashSet<int>());
        }

        #endregion
    }
}