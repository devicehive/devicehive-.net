using System;
using DeviceHive.WebSockets.Core.Network;

namespace DeviceHive.WebSockets.Core.Hosting
{
    public class ConnectionOpenedMessage
    {
        public Guid ConnectionIdentity { get; set; }

        public string Host { get; set; }

        public string Path { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ConnectionOpenedMessage()
        {
        }

        public ConnectionOpenedMessage(WebSocketConnectionBase connection)
        {
            ConnectionIdentity = connection.Identity;
            Host = connection.Host;
            Path = connection.Path;
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

    public class CloseConnectionMessage
    {
        public Guid ConnectionIdentity { get; set; }

        public CloseConnectionMessage()
        {
        }

        public CloseConnectionMessage(Guid connectionIdentity)
        {
            ConnectionIdentity = connectionIdentity;
        }
    }

    public class DataReceivedMessage
    {
        public Guid ConnectionIdentity { get; set; }

        public string Data { get; set; }

        public DataReceivedMessage()
        {
        }

        public DataReceivedMessage(Guid connectionIdentity, string data)
        {
            ConnectionIdentity = connectionIdentity;
            Data = data;
        }
    }

    public class PingReceivedMessage
    {
        public Guid ConnectionIdentity { get; set; }

        public PingReceivedMessage()
        {
        }

        public PingReceivedMessage(Guid connectionIdentity)
        {
            ConnectionIdentity = connectionIdentity;
        }
    }
    
    public class SendDataMessage
    {
        public Guid ConnectionIdentity { get; set; }

        public string Data { get; set; }

        public SendDataMessage()
        {
        }

        public SendDataMessage(Guid connectionIdentity, string data)
        {
            ConnectionIdentity = connectionIdentity;
            Data = data;
        }
    }
}