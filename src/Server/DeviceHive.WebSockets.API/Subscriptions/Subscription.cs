using DeviceHive.WebSockets.Core.Network;

namespace DeviceHive.WebSockets.API.Subscriptions
{
    public class Subscription<TKey>
    {
        public Subscription(TKey key, WebSocketConnectionBase connection)
        {
            Key = key;
            Connection = connection;
        }

        public TKey Key { get; private set; }

        public WebSocketConnectionBase Connection { get; set; }
    }
}