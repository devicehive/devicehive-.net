using System;
using DeviceHive.WebSockets.Core.Network;

namespace DeviceHive.WebSockets.Core.Hosting
{
    internal class ProxyWebSocketConnection : WebSocketConnectionBase
    {
        private readonly ApplicationService _applicationService;

        private readonly Guid _identity;
        private readonly string _host;
        private readonly string _path;

        public ProxyWebSocketConnection(ConnectionOpenedMessage connectionOpenedMessage, ApplicationService applicationService)
        {
            _applicationService = applicationService;
            _identity = connectionOpenedMessage.ConnectionIdentity;
            _host = connectionOpenedMessage.Host;
            _path = connectionOpenedMessage.Path;
        }

        #region Overrides of WebSocketConnectionBase

        public override Guid Identity
        {
            get { return _identity; }
        }

        public override string Host
        {
            get { return _host; }
        }

        public override string Path
        {
            get { return _path; }
        }

        public override void Send(string message)
        {
            _applicationService.SendMessage(_identity, message);
        }

        public override void Close()
        {
            _applicationService.CloseConnection(_identity);
        }

        #endregion
    }
}