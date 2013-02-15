using System;
using DeviceHive.WebSockets.Core.Network;

namespace DeviceHive.WebSockets.Core.Hosting
{
    public class ConnectionOpenedMessage
    {
        public WebSocketConnectionBase Connection { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ConnectionOpenedMessage()
        {
        }

        public ConnectionOpenedMessage(WebSocketConnectionBase connection)
        {
            Connection = connection;
        }
    }

    public class ConnectionClosedMessage
    {
        public Guid ConnectionIdentity { get; set; }

        public ConnectionClosedMessage()
        {
        }

        public ConnectionClosedMessage(Guid connectionIdentity)
        {
            ConnectionIdentity = connectionIdentity;
        }
    }

    public class DataMessage
    {
        public Guid ConnectionIdentity { get; set; }

        public string Data { get; set; }

        public DataMessage()
        {
        }

        public DataMessage(Guid connectionIdentity, string data)
        {
            ConnectionIdentity = connectionIdentity;
            Data = data;
        }
    }
}