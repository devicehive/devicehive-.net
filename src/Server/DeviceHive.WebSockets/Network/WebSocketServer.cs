using System;
using System.Collections.Generic;

namespace DeviceHive.WebSockets.Network
{
    public abstract class WebSocketServerBase
    {
        #region Private fields

        private readonly IDictionary<Guid, WebSocketConnectionBase> _connections =
            new Dictionary<Guid, WebSocketConnectionBase>();

        #endregion

        
        #region Working with connection list

        public IEnumerable<WebSocketConnectionBase> GetAllConnections()
        {
            return _connections.Values;
        }

        public WebSocketConnectionBase GetConnection(Guid identity)
        {
            WebSocketConnectionBase connection;
            return _connections.TryGetValue(identity, out connection) ? connection : null;
        }


        protected void RegisterConnection(WebSocketConnectionBase connection)
        {
            _connections[connection.Identity] = connection;
        }

        protected void UnregisterConnection(Guid connectionIdentity)
        {
            var connection = GetConnection(connectionIdentity);
            _connections.Remove(connectionIdentity);
            OnConnectionClosed(new WebSocketConnectionEventArgs(connection));
        }

        #endregion


        #region Start / Stop server

        public abstract void Start(string url);

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


        public event EventHandler<WebSocketConnectionEventArgs> ConnectionClosed;

        public void OnConnectionClosed(WebSocketConnectionEventArgs e)
        {
            var handler = ConnectionClosed;
            if (handler != null)
                handler(this, e);
        }

        #endregion
    }
}