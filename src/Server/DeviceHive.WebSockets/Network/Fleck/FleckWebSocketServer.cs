using Fleck;

namespace DeviceHive.WebSockets.Network.Fleck
{
    public class FleckWebSocketServer : WebSocketServerBase
    {
        private WebSocketServer _webSocketServer;

        public override void Start(string url)
        {
            _webSocketServer = new WebSocketServer(url);
            _webSocketServer.Start(c =>
            {
                c.OnOpen = () => RegisterConnection(new FleckWebSocketConnection(c));				
                c.OnClose = () => UnregisterConnection(c.ConnectionInfo.Id);
				
                c.OnMessage = msg =>
                {
                    var fc = GetConnection(c.ConnectionInfo.Id);
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