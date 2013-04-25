using System;
using System.Collections.Generic;

namespace DeviceHive.WebSockets.Core.Network
{
    public abstract class WebSocketServerBase
    {
        private readonly IDictionary<Guid, WebSocketConnectionBase> _connections =
            new Dictionary<Guid, WebSocketConnectionBase>();

        #region Connections

        public IEnumerable<WebSocketConnectionBase> GetAllConnections()
        {
            lock (_connections)
            {
                return _connections.Values;
            }
        }

        public WebSocketConnectionBase GetConnection(Guid identity)
        {
            lock (_connections)
            {
                WebSocketConnectionBase connection;
                return _connections.TryGetValue(identity, out connection) ? connection : null;
            }
        }

        protected void RegisterConnection(WebSocketConnectionBase connection)
        {
            lock (_connections)
            {
                _connections[connection.Identity] = connection;
                OnConnectionOpened(new WebSocketConnectionEventArgs(connection));
            }
        }

        protected void UnregisterConnection(Guid connectionIdentity)
        {
            WebSocketConnectionBase connection;
            lock (_connections)
            {
                connection = GetConnection(connectionIdentity);
                _connections.Remove(connectionIdentity);
            }

            if (connection != null)
                OnConnectionClosed(new WebSocketConnectionEventArgs(connection));
        }
        
        #endregion

        #region Start and Stop Server

        public abstract void Start(string url, string sslCertificateSerialNumber);

        public abstract void Stop();

        #endregion

        #region Events

        public event EventHandler<WebSocketMessageEventArgs> MessageReceived;

        protected void OnMessageReceived(WebSocketMessageEventArgs e)
        {
            var handler = MessageReceived;
            if (handler != null)
                handler(this, e);
        }


        public event EventHandler<WebSocketConnectionEventArgs> ConnectionOpened;

        protected void OnConnectionOpened(WebSocketConnectionEventArgs e)
        {
            var handler = ConnectionOpened;
            if (handler != null)
                handler(this, e);
        }


        public event EventHandler<WebSocketConnectionEventArgs> ConnectionClosed;

        protected void OnConnectionClosed(WebSocketConnectionEventArgs e)
        {
            var handler = ConnectionClosed;
            if (handler != null)
                handler(this, e);
        }

        #endregion
    }
}