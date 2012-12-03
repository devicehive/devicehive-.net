using System;
using DeviceHive.WebSockets.Controllers;
using DeviceHive.WebSockets.Network;
using Newtonsoft.Json.Linq;
using Ninject;
using log4net;

namespace DeviceHive.WebSockets.Core
{
	public sealed class Router
	{
		private readonly IKernel _kernel;

		public Router(IKernel kernel)
		{
			_kernel = kernel;
		}

		public void RouteRequest(WebSocketConnectionBase connection, string message)
		{
			try
			{
				var controller = GetController(connection);

				var request = JObject.Parse(message);
				var action = (string)request["action"];
				var args = (JObject)request["args"];

				controller.InvokeAction(connection, action, args);
			}
			catch (Exception e)
			{
				LogManager.GetLogger(typeof(Router)).Error("WebSocket request error", e);
			}			
		}

		private ControllerBase GetController(WebSocketConnectionBase connection)
		{
			switch (connection.Path)
			{
				case "/client":
					return _kernel.Get<ClientController>();

				case "/device":
					return _kernel.Get<DeviceController>();

				default:
					throw new InvalidOperationException(
						"Can't accept connections on invalid path: " + connection.Path);
			}
		}
	}
}