using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using DeviceHive.Core;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.MessageLogic;
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
    /// The service allows clients to exchange messages with the DeviceHive server using a single persistent connection.
    /// </para>
    /// <para>
    /// After connection is eshtablished, clients need to authenticate using their login and password or access key,
    /// and then start sending commands to devices using the command/insert message.
    /// As soon as a command is processed by a device, the server sends the command/update message.
    /// </para>
    /// <para>
    /// Clients may also subscribe to device notifications using the notification/subscribe message
    /// and then start receiving server-originated messages about new device notifications.
    /// </para>
    /// </summary>
    public class ClientController : ControllerBase
    {        
        #region Private Fields

        private static readonly DeviceSubscriptionManager _deviceSubscriptionManagerForNotifications = new DeviceSubscriptionManager("DeviceSubscriptions_Notification");
        private static readonly DeviceSubscriptionManager _deviceSubscriptionManagerForCommands = new DeviceSubscriptionManager("DeviceSubscriptions_Command");
        private static readonly CommandSubscriptionManager _commandSubscriptionManager = new CommandSubscriptionManager();
        
        private readonly IMessageManager _messageManager;

        #endregion

        #region Constructor

        public ClientController(ActionInvoker actionInvoker, DataContext dataContext,
            JsonMapperManager jsonMapperManager, DeviceHiveConfiguration deviceHiveConfiguration, IMessageManager messageManager) :
            base(actionInvoker, dataContext, jsonMapperManager, deviceHiveConfiguration)
        {
            _messageManager = messageManager;
        }

        #endregion

        #region Properties

        private User CurrentUser
        {
            get { return (User) Connection.Session["User"]; }
            set { Connection.Session["User"] = value; }
        }

        private AccessKey CurrentAccessKey
        {
            get { return (AccessKey)Connection.Session["AccessKey"]; }
            set { Connection.Session["AccessKey"] = value; }
        }

        #endregion

        #region ControllerBase Members

        public override void CleanupConnection(WebSocketConnectionBase connection)
        {
            base.CleanupConnection(connection);

            _deviceSubscriptionManagerForNotifications.Cleanup(connection);
            _deviceSubscriptionManagerForCommands.Cleanup(connection);
            _commandSubscriptionManager.Cleanup(connection);
        }

        #endregion

        #region Actions Methods

        /// <summary>
        /// Authenticates a client.
        /// Either login and password or accessKey parameters must be passed.
        /// </summary>
        /// <request>
        ///     <parameter name="login" type="string">User login.</parameter>
        ///     <parameter name="password" type="string">User password.</parameter>
        ///     <parameter name="accessKey" type="string">User access key.</parameter>
        /// </request>
        [Action("authenticate")]
        [AuthenticateClient]
        public void Authenticate()
        {
            if (ActionContext.GetParameter("AuthUser") == null)
                throw new WebSocketRequestException("Please specify 'login' and 'password' or 'accessKey'");

            CurrentUser = (User)ActionContext.GetParameter("AuthUser");
            CurrentAccessKey = (AccessKey)ActionContext.GetParameter("AuthAccessKey");

            SendSuccessResponse();
        }

        /// <summary>
        /// Creates new device notification on behalf of device.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="notification" cref="DeviceNotification">A <see cref="DeviceNotification"/> resource to create.</param>
        /// <response>
        ///     <parameter name="notification" cref="DeviceNotification" mode="OneWayOnly">An inserted <see cref="DeviceNotification"/> resource.</parameter>
        /// </response>
        [Action("notification/insert")]
        [AuthorizeClient(AccessKeyAction = "CreateDeviceNotification")]
        public void InsertDeviceNotification(string deviceGuid, JObject notification)
        {
            if (string.IsNullOrEmpty(deviceGuid))
                throw new WebSocketRequestException("Please specify valid deviceGuid");

            if (notification == null)
                throw new WebSocketRequestException("Please specify notification");

            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsDeviceAccessible(device, "CreateDeviceNotification"))
                throw new WebSocketRequestException("Device not found");

            var notificationEntity = GetMapper<DeviceNotification>().Map(notification);
            notificationEntity.Device = device;
            Validate(notificationEntity);

            var context = new MessageHandlerContext(notificationEntity, CurrentUser);
            _messageManager.HandleNotification(context);

            notification = GetMapper<DeviceNotification>().Map(notificationEntity, oneWayOnly: true);
            SendResponse(new JProperty("notification", notification));
        }

        /// <summary>
        /// Creates new device command.
        /// </summary>
        /// <param name="deviceGuid">Target device unique identifier.</param>
        /// <param name="command" cref="DeviceCommand">A <see cref="DeviceCommand"/> resource to create.</param>
        /// <response>
        ///     <parameter name="command" cref="DeviceCommand" mode="OneWayOnly">An inserted <see cref="DeviceCommand"/> resource.</parameter>
        /// </response>
        [Action("command/insert")]
        [AuthorizeClient(AccessKeyAction = "CreateDeviceCommand")]
        public void InsertDeviceCommand(string deviceGuid, JObject command)
        {
            if (string.IsNullOrEmpty(deviceGuid))
                throw new WebSocketRequestException("Please specify valid deviceGuid");

            if (command == null)
                throw new WebSocketRequestException("Please specify command");

            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsDeviceAccessible(device, "CreateDeviceCommand"))
                throw new WebSocketRequestException("Device not found");

            var commandEntity = GetMapper<DeviceCommand>().Map(command);
            commandEntity.Device = device;
            commandEntity.UserID = CurrentUser.ID;
            Validate(commandEntity);

            var context = new MessageHandlerContext(commandEntity, CurrentUser);
            _messageManager.HandleCommand(context);
            if (!context.IgnoreMessage)
                _commandSubscriptionManager.Subscribe(Connection, commandEntity.ID);

            command = GetMapper<DeviceCommand>().Map(commandEntity, oneWayOnly: true);
            SendResponse(new JProperty("command", command));
        }

        /// <summary>
        /// Updates an existing device command on behalf of device.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="commandId">Device command identifier.</param>
        /// <param name="command" cref="DeviceCommand">A <see cref="DeviceCommand"/> resource to update.</param>
        /// <request>
        ///     <parameter name="command.command" required="false" />
        /// </request>
        [Action("command/update")]
        [AuthorizeClient(AccessKeyAction = "UpdateDeviceCommand")]
        public void UpdateDeviceCommand(string deviceGuid, int commandId, JObject command)
        {
            if (string.IsNullOrEmpty(deviceGuid))
                throw new WebSocketRequestException("Please specify valid deviceGuid");

            if (commandId == 0)
                throw new WebSocketRequestException("Please specify valid commandId");
            
            if (command == null)
                throw new WebSocketRequestException("Please specify command");

            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsDeviceAccessible(device, "UpdateDeviceCommand"))
                throw new WebSocketRequestException("Device not found");

            var commandEntity = DataContext.DeviceCommand.Get(commandId);
            if (commandEntity == null || commandEntity.DeviceID != device.ID)
                throw new WebSocketRequestException("Device command not found");

            GetMapper<DeviceCommand>().Apply(commandEntity, command);
            commandEntity.Device = device;
            Validate(commandEntity);

            var context = new MessageHandlerContext(commandEntity, CurrentUser);
            _messageManager.HandleCommandUpdate(context);

            SendSuccessResponse();
        }

        /// <summary>
        /// Subscribes to device notifications.
        /// After subscription is completed, the server will start to send notification/insert messages to the connected user.
        /// </summary>
        /// <param name="timestamp">Timestamp of the last received notification (UTC). If not specified, the server's timestamp is taken instead.</param>
        /// <param name="deviceGuids">Array of device unique identifiers to subscribe to. If not specified, the subscription is made to all accessible devices.</param>
        /// <param name="names">Array of notification names to subscribe to.</param>
        /// <response>
        ///     <parameter name="subscriptionId" type="guid">A unique identifier of the subscription made.</parameter>
        /// </response>
        [Action("notification/subscribe")]
        [AuthorizeClient(AccessKeyAction = "GetDeviceNotification")]
        public void SubsrcibeToDeviceNotifications(DateTime? timestamp, string[] deviceGuids = null, string[] names = null)
        {
            var subscriptionId = Guid.NewGuid();
            var devices = GetDevices(deviceGuids, "GetDeviceNotification");
            var deviceIds = devices == null ? null : devices.Select(d => d.ID).ToArray();

            var initialNotificationList = GetInitialNotificationList(Connection, subscriptionId);
            lock (initialNotificationList)
            {
                _deviceSubscriptionManagerForNotifications.Subscribe(subscriptionId, Connection, deviceIds, names);
                SendResponse(new JProperty("subscriptionId", subscriptionId));

                if (timestamp != null)
                {
                    var filter = new DeviceNotificationFilter { Start = timestamp, IsDateInclusive = false, Notifications = names };
                    var initialNotifications = DataContext.DeviceNotification.GetByDevices(deviceIds, filter)
                        .Where(n => IsDeviceAccessible(n.Device, "GetDeviceNotification")).ToArray();

                    foreach (var notification in initialNotifications)
                    {
                        initialNotificationList.Add(notification.ID);
                        Notify(Connection, subscriptionId, notification, notification.Device, isInitialNotification: true);
                    }
                }
            }
        }

        /// <summary>
        /// Subscribes to device commands.
        /// After subscription is completed, the server will start to send command/insert messages to the connected user.
        /// </summary>
        /// <param name="timestamp">Timestamp of the last received command (UTC). If not specified, the server's timestamp is taken instead.</param>
        /// <param name="deviceGuids">Array of device unique identifiers to subscribe to. If not specified, the subscription is made to all accessible devices.</param>
        /// <param name="names">Array of command names to subscribe to.</param>
        /// <response>
        ///     <parameter name="subscriptionId" type="guid">A unique identifier of the subscription made.</parameter>
        /// </response>
        [Action("command/subscribe")]
        [AuthorizeClient(AccessKeyAction = "GetDeviceCommand")]
        public void SubsrcibeToDeviceCommands(DateTime? timestamp, string[] deviceGuids = null, string[] names = null)
        {
            var subscriptionId = Guid.NewGuid();
            var devices = GetDevices(deviceGuids, "GetDeviceCommand");
            var deviceIds = devices == null ? null : devices.Select(d => d.ID).ToArray();

            var initialCommandList = GetInitialCommandList(Connection, subscriptionId);
            lock (initialCommandList)
            {
                _deviceSubscriptionManagerForCommands.Subscribe(subscriptionId, Connection, deviceIds, names);
                SendResponse(new JProperty("subscriptionId", subscriptionId));

                if (timestamp != null)
                {
                    var filter = new DeviceCommandFilter { Start = timestamp, IsDateInclusive = false, Commands = names };
                    var initialCommands = DataContext.DeviceCommand.GetByDevices(deviceIds, filter)
                        .Where(n => IsDeviceAccessible(n.Device, "GetDeviceCommand")).ToArray();

                    foreach (var command in initialCommands)
                    {
                        initialCommandList.Add(command.ID);
                        Notify(Connection, subscriptionId, command, command.Device, isInitialCommand: true);
                    }
                }
            }
       }

        /// <summary>
        /// Unsubscribes from device notifications.
        /// </summary>
        /// <param name="subscriptionId">An identifier of the previously made subscription to unsubscribe from.</param>
        [Action("notification/unsubscribe")]
        [AuthorizeClient(AccessKeyAction = "GetDeviceNotification")]
        public void UnsubsrcibeFromDeviceNotifications(Guid subscriptionId)
        {
            _deviceSubscriptionManagerForNotifications.Unsubscribe(Connection, subscriptionId);
            SendSuccessResponse();
        }

        /// <summary>
        /// Unsubscribes from device commands.
        /// </summary>
        /// <param name="subscriptionId">An identifier of the previously made subscription to unsubscribe from.</param>
        [Action("command/unsubscribe")]
        [AuthorizeClient(AccessKeyAction = "GetDeviceCommand")]
        public void UnsubsrcibeFromDeviceCommands(Guid subscriptionId)
        {
            _deviceSubscriptionManagerForCommands.Unsubscribe(Connection, subscriptionId);
            SendSuccessResponse();
        }

        /// <summary>
        /// Gets meta-information about the current API.
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
                RestServerUrl = restEndpoint.Url,
            };

            SendResponse(new JProperty("info", GetMapper<ApiInfo>().Map(apiInfo)));
        }

        #endregion

        #region Notification Subscription Handling

        /// <summary>
        /// Notifies the user about new device notification.
        /// </summary>
        /// <action>notification/insert</action>
        /// <response>
        ///     <parameter name="subscriptionId" type="guid">Identifier of the associated subscription.</parameter>
        ///     <parameter name="deviceGuid" type="string">Device unique identifier.</parameter>
        ///     <parameter name="notification" cref="DeviceNotification">A <see cref="DeviceNotification"/> resource representing the notification.</parameter>
        /// </response>
        public void HandleDeviceNotification(int deviceId, int notificationId)
        {
            var subscriptions = _deviceSubscriptionManagerForNotifications.GetSubscriptions(deviceId);
            if (subscriptions.Any())
            {
                Device device = null;
                var notification = DataContext.DeviceNotification.Get(notificationId);
                foreach (var subscription in subscriptions)
                {
                    var names = (string[])subscription.Data;
                    if (names != null && !names.Contains(notification.Notification))
                        continue;

                    if (device == null)
                        device = DataContext.Device.Get(deviceId);

                    Notify(subscription.Connection, subscription.Id, notification, device);
                }
            }
        }

        private void Notify(WebSocketConnectionBase connection, Guid subscriptionId, DeviceNotification notification, Device device,
            bool isInitialNotification = false)
        {
            if (!isInitialNotification)
            {
                var initialNotificationList = GetInitialNotificationList(connection, subscriptionId);
                lock (initialNotificationList) // wait until all initial notifications are sent
                {
                    if (initialNotificationList.Contains(notification.ID))
                        return;
                }

                if (!IsDeviceAccessible(connection, device, "GetDeviceNotification"))
                    return;
            }

            connection.SendResponse("notification/insert",
                new JProperty("subscriptionId", subscriptionId),
                new JProperty("deviceGuid", device.GUID),
                new JProperty("notification", GetMapper<DeviceNotification>().Map(notification)));
        }

        #endregion

        #region Command Subscription Handling

        /// <summary>
        /// Notifies the user about new device command.
        /// </summary>
        /// <action>command/insert</action>
        /// <response>
        ///     <parameter name="subscriptionId" type="guid">Identifier of the associated subscription.</parameter>
        ///     <parameter name="deviceGuid" type="string">Device unique identifier.</parameter>
        ///     <parameter name="command" cref="DeviceCommand">A <see cref="DeviceCommand"/> resource representing the command.</parameter>
        /// </response>
        public void HandleDeviceCommand(int deviceId, int commandId)
        {
            var subscriptions = _deviceSubscriptionManagerForCommands.GetSubscriptions(deviceId);
            if (subscriptions.Any())
            {
                Device device = null;
                var command = DataContext.DeviceCommand.Get(commandId);

                foreach (var subscription in subscriptions)
                {
                    var names = (string[])subscription.Data;
                    if (names != null && !names.Contains(command.Command))
                        continue;

                    if (device == null)
                        device = DataContext.Device.Get(deviceId);

                    Notify(subscription.Connection, subscription.Id, command, device);
                }
            }
        }

        private void Notify(WebSocketConnectionBase connection, Guid subscriptionId, DeviceCommand command, Device device,
            bool isInitialCommand = false)
        {
            if (!isInitialCommand)
            {
                var initialCommandList = GetInitialCommandList(connection, subscriptionId);
                lock (initialCommandList) // wait until all initial commands are sent
                {
                    if (initialCommandList.Contains(command.ID))
                        return;
                }

                if (!IsDeviceAccessible(connection, device, "GetDeviceCommand"))
                    return;
            }

            connection.SendResponse("command/insert",
                new JProperty("subscriptionId", subscriptionId),
                new JProperty("deviceGuid", device.GUID),
                new JProperty("command", GetMapper<DeviceCommand>().Map(command)));
        }

        #endregion

        #region Command Update Subscription Handling

        /// <summary>
        /// Notifies the user about a command has been processed by a device.
        /// These messages are sent only for commands created by the current user within the current connection.
        /// </summary>
        /// <action>command/update</action>
        /// <response>
        ///     <parameter name="command" cref="DeviceCommand">A <see cref="DeviceCommand"/> resource representing the processed command.</parameter>
        /// </response>
        public void HandleCommandUpdate(int commandId)
        {
            var subscriptions = _commandSubscriptionManager.GetSubscriptions(commandId);
            if (subscriptions.Any())
            {
                var command = DataContext.DeviceCommand.Get(commandId);
                foreach (var subscription in subscriptions)
                {
                    subscription.Connection.SendResponse("command/update",
                        new JProperty("command", GetMapper<DeviceCommand>().Map(command)));
                }
            }
        }

        #endregion

        #region Private Methods

        private bool IsDeviceAccessible(Device device, string accessKeyAction)
        {
            return IsDeviceAccessible(Connection, device, accessKeyAction);
        }
        
        private bool IsDeviceAccessible(WebSocketConnectionBase connection, Device device, string accessKeyAction)
        {
            var user = (User)connection.Session["User"];
            if (user == null)
                return false;

            if (user.Role != (int)UserRole.Administrator)
            {
                if (device.NetworkID == null)
                    return false;

                var userNetworks = (List<UserNetwork>)connection.Session["UserNetworks"];
                if (userNetworks == null)
                {
                    userNetworks = DataContext.UserNetwork.GetByUser(user.ID);
                    connection.Session["UserNetworks"] = userNetworks;
                }

                if (!userNetworks.Any(un => un.NetworkID == device.NetworkID))
                    return false;
            }

            // check if access key permissions are sufficient
            var accessKey = (AccessKey)connection.Session["AccessKey"];
            return accessKey == null || accessKey.Permissions.Any(p =>
                p.IsActionAllowed(accessKeyAction) && p.IsAddressAllowed(connection.Host) &&
                p.IsNetworkAllowed(device.NetworkID) && p.IsDeviceAllowed(device.GUID));
        }

        private Device[] GetDevices(string[] deviceGuids, string accessKeyAction)
        {
            if (deviceGuids == null)
                return null;

            return deviceGuids.Select(deviceGuid =>
                {
                    var device = DataContext.Device.Get(deviceGuid);
                    if (device == null || !IsDeviceAccessible(device, accessKeyAction))
                        throw new WebSocketRequestException("Invalid deviceGuid: " + deviceGuid);

                    return device;
                }).ToArray();
        }

        private ISet<int> GetInitialNotificationList(WebSocketConnectionBase connection, Guid subscriptionId)
        {
            return (ISet<int>)connection.Session.GetOrAdd("InitialNotifications_" + subscriptionId, () => new HashSet<int>());
        }

        private ISet<int> GetInitialCommandList(WebSocketConnectionBase connection, Guid subscriptionId)
        {
            return (ISet<int>)connection.Session.GetOrAdd("InitialCommands_" + subscriptionId, () => new HashSet<int>());
        }

        #endregion
    }
}