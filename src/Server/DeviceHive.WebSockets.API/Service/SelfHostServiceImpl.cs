using System;
using System.Configuration;
using DeviceHive.Core;
using DeviceHive.WebSockets.Core.ActionsFramework;
using DeviceHive.WebSockets.Core.Network;

namespace DeviceHive.WebSockets.API.Service
{
    public class SelfHostServiceImpl
    {
        private readonly WebSocketServerBase _server;
        private readonly DeviceHiveConfiguration _configuration;

        public SelfHostServiceImpl(DeviceHiveConfiguration configuration, WebSocketServerBase server, Router router)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration");
            if (server == null)
                throw new ArgumentNullException("server");
            if (router == null)
                throw new ArgumentNullException("router");

            _configuration = configuration;
            _server = server;
            _server.ConnectionOpened += (s, e) => router.HandleNewConnection(e.Connection);
            _server.MessageReceived += (s, e) => router.RouteRequest(e.Connection, e.Message);
            _server.PingReceived += (s, e) => router.RoutePing(e.Connection);
            _server.ConnectionClosed += (s, e) => router.CleanupConnection(e.Connection);
        }

        public void Start()
        {
            var url = _configuration.WebSocketEndpoint.Url;
            if (string.IsNullOrEmpty(url))
                throw new ConfigurationErrorsException("Please specify WebSocket listening url in the webSocketEndpoint configuration element!");

            var sslCertSerialNumber = _configuration.WebSocketEndpoint.SslCertSerialNumber;
            if (string.IsNullOrEmpty(sslCertSerialNumber))
                sslCertSerialNumber = null;

            _server.Start(url, sslCertSerialNumber);
        }

        public void Stop()
        {
            _server.Stop();
        }
    }
}
