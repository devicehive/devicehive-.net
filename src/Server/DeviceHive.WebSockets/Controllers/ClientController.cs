using System;
using System.Linq;
using DeviceHive.Core.Mapping;
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
    public class ClientController : ControllerBase
    {        
        #region Private fields

        private const int _maxLoginAttempts = 10;

        private readonly DeviceSubscriptionManager _subscriptionManager;
        private readonly CommandSubscriptionManager _commandSubscriptionManager;
        private readonly MessageBus _messageBus;

        #endregion

        #region Constructor

        public ClientController(ActionInvoker actionInvoker, WebSocketServerBase server,
            DataContext dataContext, JsonMapperManager jsonMapperManager,
            [Named("DeviceNotification")] DeviceSubscriptionManager subscriptionManager,
            CommandSubscriptionManager commandSubscriptionManager,
            MessageBus messageBus) :
            base(actionInvoker, server, dataContext, jsonMapperManager)
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

        #region Methods

        #region Overrides

        public override bool IsAuthenticated
        {
            get { return CurrentUser != null; }
        }

        #endregion

        #region Actions

        [Action("authenticate")]
        public void Authenticate()
        {
            var login = (string) ActionArgs["login"];
            var password = (string) ActionArgs["password"];

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

        [Action("command/insert", NeedAuthentication = true)]
        public void InsertDeviceCommand()
        {
            var deviceGuid = Guid.Parse((string) ActionArgs["deviceGuid"]);
            var commandObj = (JObject) ActionArgs["command"];

            var device = DataContext.Device.Get(deviceGuid);
            if (device == null || !IsNetworkAccessible(device.NetworkID))
                throw new WebSocketRequestException("Device not found");

            var command = CommandMapper.Map(commandObj);
            command.Device = device;
            Validate(command);

            DataContext.DeviceCommand.Save(command);
            _commandSubscriptionManager.Subscribe(Connection, command.ID);
            _messageBus.Notify(new DeviceCommandAddedMessage(deviceGuid, command.ID));
            
            commandObj = CommandMapper.Map(command);
            SendResponse(new JProperty("command", commandObj));
        }

        [Action("notification/subscribe", NeedAuthentication = true)]
        public void SubsrcibeToDeviceNotifications()
        {
            var deviceGuids = ParseDeviceGuids();
            foreach (var deviceGuid in deviceGuids)
                _subscriptionManager.Subscribe(Connection, deviceGuid);

            SendSuccessResponse();
        }

        [Action("notification/unsubscribe", NeedAuthentication = true)]
        public void UnsubsrcibeFromDeviceNotifications()
        {
            var deviceGuids = ParseDeviceGuids();
            foreach (var deviceGuid in deviceGuids)
                _subscriptionManager.Unsubscribe(Connection, deviceGuid);

            SendSuccessResponse();
        }

        #endregion

        #region Notification handling

        public void HandleDeviceNotification(Guid deviceGuid, int notificationId)
        {
            var notification = DataContext.DeviceNotification.Get(notificationId);
            var device = DataContext.Device.Get(deviceGuid);
            var connections = _subscriptionManager.GetConnections(deviceGuid);

            foreach (var connection in connections)
                Notify(connection, notification, device);
        }

        public void CleanupNotifications(WebSocketConnectionBase connection)
        {
            _subscriptionManager.Cleanup(connection);
        }

        private void Notify(WebSocketConnectionBase connection, DeviceNotification notification, Device device)
        {
            var user = (User) connection.Session["user"];
            if (user == null || !IsNetworkAccessible(device.NetworkID, user))
                return;

            SendResponse(connection, "notification/insert",
                new JProperty("notification", NotificationMapper.Map(notification)));
        }

        #endregion

        #region Command update handle

        public void HandleCommandUpdate(int commandId)
        {
            var command = DataContext.DeviceCommand.Get(commandId);
            var connections = _commandSubscriptionManager.GetConnections(commandId);

            foreach (var connection in connections)
            {
                SendResponse(connection, "command/update",
                    new JProperty("command", CommandMapper.Map(command)));
            }
        }

        #endregion

        #region Helper methods

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
            user.LoginAttempts = 0;
            user.LastLogin = DateTime.UtcNow;
            DataContext.User.Save(user);
        }

        #endregion

        #endregion
    }
}