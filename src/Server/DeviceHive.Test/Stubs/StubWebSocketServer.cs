using System;
using DeviceHive.WebSockets.Core.Network;

namespace DeviceHive.Test.Stubs
{
    public class StubWebSocketServer : WebSocketServerBase
    {
        public override void Start(string url, string sslCertificateSerialNumber)
        {
        }

        public override void Stop()
        {
        }

        public StubWebSocketConnection Connect(string path)
        {
            var connection = new StubWebSocketConnection(Guid.NewGuid(), path);
            connection.ReceiveMessageHandler = msg =>
                OnMessageReceived(new WebSocketMessageEventArgs(connection, msg));
            RegisterConnection(connection);
            return connection;
        }

        public void Disconnect(StubWebSocketConnection connection)
        {
            UnregisterConnection(connection.Identity);
        }
    }

    public class StubWebSocketConnection : WebSocketConnectionBase
    {
        private readonly Guid _identity;
        private readonly string _path;

        public StubWebSocketConnection(Guid identity, string path)
        {
            _identity = identity;
            _path = path;
        }

        public override Guid Identity
        {
            get { return _identity; }
        }

        public override string Host
        {
            get { return string.Empty; }
        }

        public override string Path
        {
            get { return _path; }
        }

        public Action<string> SendMessageHandler { get; set; }

        public Action<string> ReceiveMessageHandler { get; set; }

        public override void Send(string message)
        {
            if (SendMessageHandler != null)
                SendMessageHandler(message);
        }

        public override void Close()
        {            
        }

        public void Receive(string message)
        {
            if (ReceiveMessageHandler != null)
                ReceiveMessageHandler(message);
        }
    }
}