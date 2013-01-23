using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Fleck;
using log4net;

namespace DeviceHive.WebSockets.Network.Fleck
{
    public class FleckWebSocketServer : WebSocketServerBase
    {
        private WebSocketServer _webSocketServer;

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

            _webSocketServer.Start(c =>
            {
                c.OnOpen = () => RegisterConnection(new FleckWebSocketConnection(c));                
                c.OnClose = () => UnregisterConnection(c.ConnectionInfo.Id);
                
                c.OnMessage = msg =>
                {
                    var fc = GetConnection(c.ConnectionInfo.Id);
                    if (fc == null)
                    {                        
                        LogManager.GetLogger(GetType())
                            .ErrorFormat("Connection {0} isn't registered", c.ConnectionInfo.Id);
                        return;
                    }

                    var e = new WebSocketMessageEventArgs(fc, msg);
                    OnMessageReceived(e);
                };
            });
        }

        public override void Stop()
        {
            _webSocketServer.Dispose();
        }
    }
}