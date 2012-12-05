using System.Configuration;
using DeviceHive.WebSockets.ActionsFramework;
using DeviceHive.WebSockets.Network;

namespace DeviceHive.WebSockets
{
    public class WebSocketServiceImpl
    {
        private readonly WebSocketServerBase _server;

        public WebSocketServiceImpl(WebSocketServerBase server, Router router)
        {
            _server = server;
            _server.MessageReceived += (s, e) => router.RouteRequest(e.Connection, e.Message);
            _server.ConnectionClosed += (s, e) => router.CleanupConnection(e.Connection);
        }        

        public void Start()
        {
            var url = ConfigurationManager.AppSettings["webSocketListenUrl"];
            _server.Start(url);
        }

        public void Stop()
        {
            _server.Stop();            
        }        
    }
}
