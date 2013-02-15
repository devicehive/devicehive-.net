using System;
using System.Configuration;
using DeviceHive.WebSockets.Core.Network;

namespace DeviceHive.WebSockets.Host
{
    internal class HostServiceImpl
    {
        private readonly ApplicationCollection _applications = new ApplicationCollection();
        private readonly WebSocketServerBase _server;

        
        public HostServiceImpl(WebSocketServerBase server)
        {
            _server = server;
            _server.ConnectionOpened += OnConnectionOpened;
            _server.MessageReceived += OnMessageReceived;
            _server.ConnectionClosed += OnConnectionClosed;
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


        private void OnConnectionOpened(object sender, WebSocketConnectionEventArgs args)
        {
            
        }

        private void OnMessageReceived(object sender, WebSocketMessageEventArgs args)
        {
            throw new NotImplementedException();
        }

        private void OnConnectionClosed(object sender, WebSocketConnectionEventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}