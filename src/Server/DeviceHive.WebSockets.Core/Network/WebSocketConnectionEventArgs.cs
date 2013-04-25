using System;

namespace DeviceHive.WebSockets.Core.Network
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