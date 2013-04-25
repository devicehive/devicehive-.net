using System.Configuration;
using DeviceHive.WebSockets.Core.ActionsFramework;
using DeviceHive.WebSockets.Core.Network;

namespace DeviceHive.WebSockets.API.Service
{
    public class SelfHostServiceImpl
    {
        private readonly WebSocketServerBase _server;

        public SelfHostServiceImpl(WebSocketServerBase server, Router router)
        {
            _server = server;
            _server.ConnectionOpened += (s, e) => router.HandleNewConnection(e.Connection);
            _server.MessageReceived += (s, e) => router.RouteRequest(e.Connection, e.Message);
            _server.ConnectionClosed += (s, e) => router.CleanupConnection(e.Connection);
        }

        public void Start()
        {
            var url = ConfigurationManager.AppSettings["webSocketListenUrl"];
            var sslCertificateSerialNumber = ConfigurationManager.AppSettings["webSocketCertificateSerialNumber"];

            _server.Start(url, sslCertificateSerialNumber);
        }

        public void Stop()
        {
            _server.Stop();
        }
    }
}
