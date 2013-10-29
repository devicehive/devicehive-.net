using DeviceHive.WebSockets.Core.Network;

namespace DeviceHive.WebSockets.API.Subscriptions
{
    public class Subscription<TKey>
    {
        public Subscription(TKey key, WebSocketConnectionBase connection, object data = null)
        {
            Key = key;
            Connection = connection;
            Data = data;
        }

        public TKey Key { get; private set; }

        public WebSocketConnectionBase Connection { get; set; }

        public object Data { get; set; }
    }
}