using DeviceHive.WebSockets.Core.Network;
using System;

namespace DeviceHive.WebSockets.API.Subscriptions
{
    public class Subscription<TKey>
    {
        public Subscription(Guid id, WebSocketConnectionBase connection, TKey[] keys, object data = null)
        {
            Id = id;
            Connection = connection;
            Keys = keys;
            Data = data;
        }

        public Guid Id { get; private set; }

        public WebSocketConnectionBase Connection { get; private set; }

        public TKey[] Keys { get; private set; }

        public object Data { get; set; }
    }
}