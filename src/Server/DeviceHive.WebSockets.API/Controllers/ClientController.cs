using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.Messaging;
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

        private readonly DeviceSubscriptionManager _subscriptionManager;
        private readonly CommandSubscriptionManager _commandSubscriptionManager;
        private readonly MessageBus _messageBus;

        #endregion

        #region Constructor

        public ClientController(ActionInvoker actionInvoker,
            DataContext dataContext, JsonMapperManager jsonMapperManager,
            [Named("DeviceNotification")] DeviceSubscriptionManager subscriptionManager,
            CommandSubscriptionManager commandSubscriptionManager,
            MessageBus messageBus) :
            base(actionInvoker, dataContext, jsonMapperManager)
        {
            _subscriptionManager = subscriptionManager;
            _commandSubscriptionManager = commandSubscriptionManager;
            _messageBus = messageBus;
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
            CleanupNotifications(connection);
        }

        #endregion

        #region Actions Methods

        /// <summary>
        /// Authenticates a user.
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
        /// Creates new device command.
        /// </summary>
        /// <param name="deviceGuid">Target device unique identifier.</param>
        /// <param name="command" cref="DeviceCommand">A <see cref="DeviceCommand"/> resource to create.</param>
        /// <response>
        ///     <parameter name="command" cref="DeviceCommand" mode="OneWayOnly">An inserted <see cref="DeviceCommand"/> resource.</parameter>
        /// </response>
        [Action("command/insert")]
        [AuthorizeClient(AccessKeyAction = "CreateDeviceCommand")]
        public void InsertDeviceCommand(Guid deviceGuid, JObject command)
        {
            if (deviceGuid == Guid.Empty)
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

            DataContext.DeviceCommand.Save(commandEntity);
            _commandSubscriptionManager.Subscribe(Connection, commandEntity.ID);
            _messageBus.Notify(new DeviceCommandAddedMessage(device.ID, commandEntity.ID));

            command = GetMapper<DeviceCommand>().Map(commandEntity, oneWayOnly: true);
            SendResponse(new JProperty("command", command));
        }

        /// <summary>
        /// Subscribes to device notifications.
        /// After subscription is completed, the server will start to send notification/insert messages to the connected user.
        /// </summary>
        /// <param name="timestamp">Timestamp of the last received notification (UTC). If not specified, the server's timestamp is taken instead.</param>
        /// <request>
        ///     <parameter name="deviceGuids" type="guid[]">Array of device unique identifiers to subscribe to. If not specified, the subscription is made to all accessible devices.</parameter>
        /// </request>
        [Action("notification/subscribe")]
        [AuthorizeClient(AccessKeyAction = "GetDeviceNotification")]
        public void SubsrcibeToDeviceNotifications(DateTime? timestamp)
        {
            var devices = GetSubscriptionDevices("GetDeviceNotification").ToArray();

            if (timestamp != null)
                SendInitialNotifications(devices, timestamp);

            foreach (var deviceId in GetSubscriptionDeviceIds(devices))
                _subscriptionManager.Subscribe(Connection, deviceId);

            SendSuccessResponse();
        }

        /// <summary>
        /// Unsubscribes from device notifications.
        /// </summary>
        /// <request>
        ///     <parameter name="deviceGuids" type="guid[]">Array of device unique identifiers to unsubscribe from. Keep null to unsubscribe from previously made subscription to all accessible devices.</parameter>
        /// </request>
        [Action("notification/unsubscribe")]
        [AuthorizeClient(AccessKeyAction = "GetDeviceNotification")]
        public void UnsubsrcibeFromDeviceNotifications()
        {
            var devices = GetSubscriptionDevices("GetDeviceNotification").ToArray();
            foreach (var deviceId in GetSubscriptionDeviceIds(devices))
                _subscriptionManager.Unsubscribe(Connection, deviceId);

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
            var apiInfo = new ApiInfo
            {
                ApiVersion = DeviceHive.Core.Version.ApiVersion,
                ServerTimestamp = DataContext.Timestamp.GetCurrentTimestamp(),
                RestServerUrl = ConfigurationManager.AppSettings["RestServerUrl"]
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
        ///     <parameter name="deviceGuid" type="guid">Device unique identifier.</parameter>
        ///     <parameter name="notification" cref="DeviceNotification">A <see cref="DeviceNotification"/> resource representing the notification.</parameter>
        /// </response>
        public void HandleDeviceNotification(int deviceId, int notificationId)
        {
            var connections = _subscriptionManager.GetConnections(deviceId);
            if (connections.Any())
            {
                var notification = DataContext.DeviceNotification.Get(notificationId);
                var device = DataContext.Device.Get(deviceId);

                foreach (var connection in connections)
                    Notify(connection, notification, device);
            }
        }

        private void CleanupNotifications(WebSocketConnectionBase connection)
        {
            _subscriptionManager.Cleanup(connection);
        }

        private void SendInitialNotifications(Device[] devices, DateTime? timestamp)
        {
            var initialNotificationList = GetInitialNotificationList(Connection);

            lock (initialNotificationList)
            {
                var filter = new DeviceNotificationFilter { Start = timestamp, IsDateInclusive = false };
                var deviceIds = devices.Length == 1 && devices[0] == null ? null : devices.Select(d => d.ID).ToArray();
                var initialNotifications = DataContext.DeviceNotification.GetByDevices(deviceIds, filter)
                    .Where(n => IsDeviceAccessible(n.Device, "GetDeviceNotification")).ToArray();

                foreach (var notification in initialNotifications)
                {
                    initialNotificationList.Add(notification.ID);
                    Notify(Connection, notification, notification.Device, isInitialNotification: true);
                }
            }
        }

        private void Notify(WebSocketConnectionBase connection, DeviceNotification notification, Device device,
            bool isInitialNotification = false)
        {
            if (!isInitialNotification)
            {
                var initialNotificationList = GetInitialNotificationList(connection);
                lock (initialNotificationList)
                {
                    if (initialNotificationList.Contains(notification.ID))
                        return;
                }

                if (!IsDeviceAccessible(connection, device, "GetDeviceNotification"))
                    return;
            }

            connection.SendResponse("notification/insert",
                new JProperty("deviceGuid", device.GUID),
                new JProperty("notification", GetMapper<DeviceNotification>().Map(notification)));
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
            var connections = _commandSubscriptionManager.GetConnections(commandId);
            if (connections.Any())
            {
                var command = DataContext.DeviceCommand.Get(commandId);
                foreach (var connection in connections)
                {
                    connection.SendResponse("command/update",
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
                p.IsNetworkAllowed(device.NetworkID ?? 0) && p.IsDeviceAllowed(device.GUID.ToString()));
        }

        private IEnumerable<Device> GetSubscriptionDevices(string accessKeyAction)
        {
            var deviceGuids = ParseDeviceGuids();
            if (deviceGuids == null)
            {
                yield return null;
                yield break;
            }

            foreach (var deviceGuid in deviceGuids)
            {
                var device = DataContext.Device.Get(deviceGuid);
                if (device == null || !IsDeviceAccessible(device, accessKeyAction))
                    throw new WebSocketRequestException("Invalid deviceGuid: " + deviceGuid);

                yield return device;
            }
        }

        private IEnumerable<int?> GetSubscriptionDeviceIds(IEnumerable<Device> devices)
        {
            foreach (var device in devices)
                yield return device != null ? (int?)device.ID : null;
        }

        private IEnumerable<Guid> ParseDeviceGuids()
        {
            if (ActionContext.Request == null)
                return null;

            var deviceGuids = ActionContext.Request["deviceGuids"];
            if (deviceGuids == null)
                return null;

            var deviceGuidsArray = deviceGuids as JArray;
            if (deviceGuidsArray != null)
                return deviceGuidsArray.Select(t => (Guid)t).ToArray();

            return new[] { (Guid)deviceGuids };
        }

        private ISet<int> GetInitialNotificationList(WebSocketConnectionBase connection)
        {
            return (ISet<int>)connection.Session.GetOrAdd("InitialNotifications", () => new HashSet<int>());
        }

        #endregion
    }
}