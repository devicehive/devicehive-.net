using System.Configuration;
using System.ServiceProcess;
using DeviceHive.Core.Messaging;
using DeviceHive.WebSockets.Controllers;
using DeviceHive.WebSockets.Core;
using DeviceHive.WebSockets.Network;

namespace DeviceHive.WebSockets
{
	public partial class WebSocketService : ServiceBase
	{
		private readonly WebSocketServerBase _server;

		private readonly ClientController _clientController;
		private readonly DeviceController _deviceController;		

		public WebSocketService(WebSocketServerBase server,
			ClientController clientController, DeviceController deviceController,
			Router router, MessageBus messageBus)
		{			
			InitializeComponent();
			
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

		
		protected override void OnStart(string[] args)
		{
			var url = ConfigurationManager.AppSettings["webSocketListenUrl"];
			_server.Start(url);
		}

		protected override void OnStop()
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
