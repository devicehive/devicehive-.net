using System;
using Fleck;

namespace DeviceHive.WebSockets.Network.Fleck
{
    public class FleckWebSocketConnection : WebSocketConnectionBase
    {
        private readonly IWebSocketConnection _fleckConnection;

        public FleckWebSocketConnection(IWebSocketConnection fleckConnection)
        {
            _fleckConnection = fleckConnection;
        }

        #region Overrides of WebSocketConnectionBase

        public override Guid Identity
        {
            get { return _fleckConnection.ConnectionInfo.Id; }
        }

        public override string Path
        {
            get { return _fleckConnection.ConnectionInfo.Path; }
        }

        public override void Send(string message)
        {
            _fleckConnection.Send(message);
        }

        #endregion
    }
}