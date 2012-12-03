using System.Configuration;
using DeviceHive.Core.Messaging;
using DeviceHive.WebSockets.Controllers;
using DeviceHive.WebSockets.Core;
using DeviceHive.WebSockets.Network;

namespace DeviceHive.WebSockets
{
	public class WebSocketServiceImpl
	{
		private readonly WebSocketServerBase _server;

		private readonly ClientController _clientController;
		private readonly DeviceController _deviceController;

        public WebSocketServiceImpl(WebSocketServerBase server,
			ClientController clientController, DeviceController deviceController,
			Router router, MessageBus messageBus)
		{
			_clientController = clientController;
			_deviceController = deviceController;

			_server = server;
			_server.MessageReceived += (s, e) => router.RouteRequest(e.Connection, e.Message);
		    _server.ConnectionClosed += (s, e) =>
		    {
                _clientController.CleanupNotifications(e.Connection);
                _deviceController.CleanupNotifications(e.Connection);
		    };

			messageBus.Subscribe((DeviceNotificationAddedMessage msg) => HandleNewNotification(msg));
			messageBus.Subscribe((DeviceCommandAddedMessage msg) => HandleNewCommand(msg));
		}


	    public void Start()
		{
			var url = ConfigurationManager.AppSettings["webSocketListenUrl"];
			_server.Start(url);
		}

		public void Stop()
		{
			_server.Stop();
            
		}


		private void HandleNewCommand(DeviceCommandAddedMessage message)
		{
            _deviceController.HandleDeviceCommand(message.DeviceGuid, message.CommandId);
		}

		private void HandleNewNotification(DeviceNotificationAddedMessage message)
		{
			_clientController.HandleDeviceNotification(message.DeviceGuid, message.NotificationId);
		}
	}
}
