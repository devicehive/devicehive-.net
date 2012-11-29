using System;

namespace DeviceHive.WebSockets.Network
{
    public class WebSocketConnectionEventArgs : EventArgs
    {
        public WebSocketConnectionEventArgs(WebSocketConnectionBase connection)
		{
			Connection = connection;
		}

		public WebSocketConnectionBase Connection { get; private set; }
    }
}