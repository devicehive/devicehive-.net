using System;
using System.Linq;
using DeviceHive.Core;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.Messaging;
using DeviceHive.Data;
using DeviceHive.Data.Model;
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

	    private readonly SubscriptionManager _subscriptionManager;
	    private readonly MessageBus _messageBus;

	    #endregion

		#region Constructor

		public ClientController(DataContext dataContext, WebSocketServerBase server,
			JsonMapperManager jsonMapperManager,
			[Named("DeviceNotification")] SubscriptionManager subscriptionManager,
			MessageBus messageBus) :
			base(dataContext, server, jsonMapperManager)
		{
			_subscriptionManager = subscriptionManager;
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

		#region Actions

		protected override void InvokeActionImpl()
		{
			if (CurrentUser == null && ActionName != "authenticate")
				return;

			switch (ActionName)
			{
				case "authenticate":
					Authenticate();
					break;

				case "command/insert":
					InsertDeviceCommand();
					break;
				
				case "notification/subscribe":
					SubsrcibeToDeviceNotifications();
					break;

				case "notification/unsubscribe":
					UnsubsrcibeFromDeviceNotifications();
					break;
			}
		}		
		
		private void Authenticate()
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

		private void InsertDeviceCommand()
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
			_messageBus.Notify(new DeviceCommandAddedMessage(deviceGuid, command.ID));
			
			commandObj = CommandMapper.Map(command);
			SendResponse(new JProperty("command", commandObj));
		}		

		private void SubsrcibeToDeviceNotifications()
		{
			var deviceGuids = ParseDeviceGuids();
			foreach (var deviceGuid in deviceGuids)
				_subscriptionManager.Subscribe(Connection, deviceGuid);

			SendSuccessResponse();
		}

		private void UnsubsrcibeFromDeviceNotifications()
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

			SendResponse(connection, "notification/notify",
				new JProperty("notification", NotificationMapper.Map(notification)));
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

        #endregion

        #endregion
    }
}