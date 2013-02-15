using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Fleck;
using log4net;

namespace DeviceHive.WebSockets.Core.Network.Fleck
{
    public class FleckWebSocketServer : WebSocketServerBase
    {
        private readonly ILog _logger;
        private WebSocketServer _webSocketServer;

        #region Constructor

        public FleckWebSocketServer()
        {
            _logger = LogManager.GetLogger(GetType());
        }
        #endregion

        #region WebSocketServerBase Members

        public override void Start(string url, string sslCertificateSerialNumber)
        {
            _webSocketServer = new WebSocketServer(url);

            if (sslCertificateSerialNumber != null)
            {
                var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                var certificate = store.Certificates
                    .Find(X509FindType.FindBySerialNumber, sslCertificateSerialNumber, false)
                    .OfType<X509Certificate2>()
                    .FirstOrDefault();

                _webSocketServer.Certificate = certificate;
            }

            _logger.Info("Starting WebSocket server");
            _webSocketServer.Start(c =>
            {
                c.OnOpen = () =>
                    {
                        _logger.Debug("Opened connection: " + c.ConnectionInfo.Id);
                        RegisterConnection(new FleckWebSocketConnection(c));
                    };
                c.OnClose = () =>
                    {
                        _logger.Debug("Closed connection: " + c.ConnectionInfo.Id);
                        UnregisterConnection(c.ConnectionInfo.Id);
                    };
                
                c.OnMessage = msg =>
                    {
                        _logger.Debug("Received message for connection: " + c.ConnectionInfo.Id);

                        var fc = WaitConnection(c.ConnectionInfo.Id);
                        if (fc == null)
                        {
                            _logger.ErrorFormat("Connection {0} is not registered", c.ConnectionInfo.Id);
                            return;
                        }

                        var e = new WebSocketMessageEventArgs(fc, msg);
                        OnMessageReceived(e);
                    };
            });
        }

        public override void Stop()
        {
            _logger.Info("Stopping WebSocket server");
            _webSocketServer.Dispose();
        }
        #endregion

        #region Private Methods

        private WebSocketConnectionBase WaitConnection(Guid connectionId)
        {
            for (var i = 0; i < 10; i++)
            {
                var connection = GetConnection(connectionId);
                if (connection != null)
                    return connection;

                Thread.Sleep(50);
            }
            return null;
        }
        #endregion
    }
}