using System;
using DeviceHive.Core;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.Messaging;
using DeviceHive.Data.Model;
using DeviceHive.WebSockets.Core;
using DeviceHive.WebSockets.Network;
using DeviceHive.WebSockets.Subscriptions;
using Newtonsoft.Json.Linq;
using Ninject;

namespace DeviceHive.WebSockets.Controllers
{
	public class ClientController : ControllerBase
	{
	    #region Private fields

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

				case "command/update":
					UpdateDeviceCommand();
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
			if (user == null || !user.IsValidPassword(password))
				throw new WebSocketRequestException("Invalid login or password");

			CurrentUser = user;
			SendSuccessResponse();
		}

		private void InsertDeviceCommand()
		{
			var deviceGuid = Guid.Parse((string) ActionArgs["deviceGuid"]);
			var commandObj = (JObject) ActionArgs["device"];			

			var device = DataContext.Device.Get(deviceGuid);
			if (device == null) // todo: check that user has access to this device
				throw new WebSocketRequestException("Device not found");

			var command = CommandMapper.Map(commandObj);
			command.Device = device;
			// todo: command validation

			DataContext.DeviceCommand.Save(command);
			_messageBus.Notify(new DeviceCommandAddedMessage(deviceGuid, command.ID));
			
			commandObj = CommandMapper.Map(command);
			SendResponse(new JProperty("command", commandObj));
		}

		private void UpdateDeviceCommand()
		{
			throw new NotImplementedException();
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
			var connections = _subscriptionManager.GetConnections(deviceGuid);

			foreach (var connection in connections)
				Notify(connection, notification);
		}

		private void Notify(WebSocketConnectionBase connection, DeviceNotification notification)
		{
			// todo: check connection user and his access to the device

			SendResponse(connection, "notification/notify",
				new JProperty("notification", NotificationMapper.Map(notification)));
		}

		#endregion

		#endregion
	}
}