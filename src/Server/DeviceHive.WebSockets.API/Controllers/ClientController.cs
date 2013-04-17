using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.Messaging;
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
    /// The service allows clients to exchange messages with the DeviceHive server using a single persistent connection.
    /// </para>
    /// <para>
    /// After connection is eshtablished, clients need to authenticate using their login and password,
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

        private const int _maxLoginAttempts = 10;

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
            get { return (User) Connection.Session["user"]; }
            set { Connection.Session["user"] = value; }
        }

        #endregion

        #region ControllerBase Members

        public override bool IsAuthenticated
        {
            get { return CurrentUser != null; }
        }

        public override void CleanupConnection(WebSocketConnectionBase connection)
        {
            base.CleanupConnection(connection);
            CleanupNotifications(connection);
        }

        #endregion

        #region Actions Methods

        /// <summary>
        /// Authenticates a user.
        /// </summary>
        /// <param name="login">User login.</param>
        /// <param name="password">User password.</param>
        [Action("authenticate")]
        public void Authenticate(string login, string password)
        {
            if (login == null || password == null)
                throw new WebSocketRequestException("Please specify 'login' and 'password'");

            var user = DataContext.User.Get(login);
            if (user == null || user.Status != (int)UserStatus.Active)
                throw new WebSocketRequestException("Invalid login or password");

            if (user.IsValidPassword(password))
            {
                UpdateUserLastLogin(user);
            }
            else
            {
                IncrementUserLoginAttempts(user);
                throw new WebSocketRequestException("Invalid login or password");
            }

            CurrentUser = user;
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
        [Action("command/insert", NeedAuthentication = true)]
        public void InsertDeviceCommand(Guid deviceGuid, JObject command)
        {
            if (deviceGuid == Guid.Empty)
                throw new WebSocketRequestException("Please specify valid deviceGuid");

            if (command == null)
                throw new WebSocketRequestException("Please specify command");

            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsNetworkAccessible(device.NetworkID))
                throw new WebSocketRequestException("Device not found");

            var commandEntity = CommandMapper.Map(command);
            commandEntity.Device = device;
            commandEntity.UserID = CurrentUser.ID;
            Validate(commandEntity);

            DataContext.DeviceCommand.Save(commandEntity);
            _commandSubscriptionManager.Subscribe(Connection, commandEntity.ID);
            _messageBus.Notify(new DeviceCommandAddedMessage(device.ID, commandEntity.ID));
            
            command = CommandMapper.Map(commandEntity, oneWayOnly: true);
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
        [Action("notification/subscribe", NeedAuthentication = true)]
        public void SubsrcibeToDeviceNotifications(DateTime? timestamp)
        {
            var devices = GetSubscriptionDevices().ToArray();

            if (timestamp != null)
                SendInitialNotifications(devices, timestamp);

            var deviceIds = GetSubscriptionDeviceIds(devices);
            foreach (var deviceId in deviceIds)
                _subscriptionManager.Subscribe(Connection, deviceId);

            SendSuccessResponse();
        }

        /// <summary>
        /// Unsubscribes from device commands.
        /// </summary>
        /// <request>
        ///     <parameter name="deviceGuids" type="guid[]">Array of device unique identifiers to unsubscribe from. Keep null to unsubscribe from previously made subscription to all accessible devices.</parameter>
        /// </request>
        [Action("notification/unsubscribe", NeedAuthentication = true)]
        public void UnsubsrcibeFromDeviceNotifications()
        {
            var deviceIds = GetSubscriptionDeviceIds().ToArray();
            foreach (var deviceId in deviceIds)
                _subscriptionManager.Unsubscribe(Connection, deviceId);

            SendSuccessResponse();
        }

        #endregion

        #region Notification Handling

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

            if (devices.Length == 1 && devices[0] == null)
                devices = DataContext.Device.GetByUser(CurrentUser.ID).ToArray();

            lock (initialNotificationList)
            {
                var filter = new DeviceNotificationFilter { Start = timestamp, IsDateInclusive = false };
                var initialNotifications = DataContext.DeviceNotification.GetByDevices(
                    devices.Select(d => d.ID).ToArray(), filter);

                foreach (var notification in initialNotifications)
                {
                    initialNotificationList.Add(notification.ID);
                    Notify(Connection, notification, notification.Device, isInitialNotification: true);
                }
            }
        }

        /// <summary>
        /// Notifies the user about new device notification.
        /// </summary>
        /// <action>notification/insert</action>
        /// <response>
        ///     <parameter name="deviceGuid" type="guid">Device unique identifier.</parameter>
        ///     <parameter name="notification" cref="DeviceNotification">A <see cref="DeviceNotification"/> resource representing the notification.</parameter>
        /// </response>
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

                var user = (User) connection.Session["user"];
                if (user == null || !IsNetworkAccessible(device.NetworkID, user))
                    return;
            }

            connection.SendResponse("notification/insert",
                new JProperty("deviceGuid", device.GUID),
                new JProperty("notification", NotificationMapper.Map(notification)));
        }

        #endregion

        #region Command Update Handling

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
                        new JProperty("command", CommandMapper.Map(command)));
                }
            }
        }

        #endregion

        #region Private Methods

        private bool IsNetworkAccessible(int? networkId, User user = null)
        {
            if (user == null)
                user = CurrentUser;

            if (user.Role == (int) UserRole.Administrator)
                return true;

            if (networkId == null)
                return false;

            var userNetworks = DataContext.UserNetwork.GetByUser(user.ID);
            return userNetworks.Any(un => un.NetworkID == networkId);
        }

        private void IncrementUserLoginAttempts(User user)
        {
            user.LoginAttempts++;
            if (user.LoginAttempts >= _maxLoginAttempts)
                user.Status = (int)UserStatus.LockedOut;
            DataContext.User.Save(user);
        }

        private void UpdateUserLastLogin(User user)
        {
            // update LastLogin only if it's too far behind - save database resources
            if (user.LoginAttempts > 0 || user.LastLogin == null || user.LastLogin.Value.AddHours(1) < DateTime.UtcNow)
            {
                user.LoginAttempts = 0;
                user.LastLogin = DateTime.UtcNow;
                DataContext.User.Save(user);
            }
        }

        private IEnumerable<int?> GetSubscriptionDeviceIds(IEnumerable<Device> devices = null)
        {
            if (devices == null)
                devices = GetSubscriptionDevices();

            foreach (var device in devices)
            {
                if (device == null)
                {
                    yield return null;
                }
                else
                {
                    yield return device.ID;
                }
            }
        }

        private IEnumerable<Device> GetSubscriptionDevices()
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
                if (device == null || !IsNetworkAccessible(device.NetworkID))
                    throw new WebSocketRequestException("Invalid deviceGuid: " + deviceGuid);

                yield return device;
            }
        }

        private IEnumerable<Guid> ParseDeviceGuids()
        {
            if (ActionArgs == null)
                return null;

            var deviceGuids = ActionArgs["deviceGuids"];
            if (deviceGuids == null)
                return null;

            var deviceGuidsArray = deviceGuids as JArray;
            if (deviceGuidsArray != null)
                return deviceGuidsArray.Select(t => (Guid) t).ToArray();

            return new[] {(Guid) deviceGuids};
        }

        private ISet<int> GetInitialNotificationList(WebSocketConnectionBase connection)
        {
            return (ISet<int>)connection.Session.GetOrAdd("InitialNotifications", () => new HashSet<int>());
        }

        #endregion
    }
}