using System;

namespace DeviceHive.WebSockets.Network
{
	public class WebSocketMessageEventArgs : EventArgs
	{
		public WebSocketMessageEventArgs(WebSocketConnectionBase connection, string message)
		{
			Connection = connection;
			Message = message;
		}

		public WebSocketConnectionBase Connection { get; private set; }
		public string Message { get; private set; }
	}
}
