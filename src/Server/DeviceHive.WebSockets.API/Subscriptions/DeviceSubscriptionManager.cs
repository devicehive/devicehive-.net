using System.Collections.Generic;
using DeviceHive.WebSockets.Core.Network;

namespace DeviceHive.WebSockets.API.Subscriptions
{
    public class DeviceSubscriptionManager : SubscriptionManager<int>
    {
        public DeviceSubscriptionManager()
            : base("DeviceSubscriptions")
        {
        }

        public DeviceSubscriptionManager(string subscriptionsValueKey)
            : base(subscriptionsValueKey)
        {
        }

        public void Subscribe(WebSocketConnectionBase connection, int? deviceId, object data = null)
        {
            base.Subscribe(connection, GetKey(deviceId), data);
        }

        public void Unsubscribe(WebSocketConnectionBase connection, int? deviceId)
        {
            base.Unsubscribe(connection, GetKey(deviceId));
        }

        public IEnumerable<Subscription<int>> GetSubscriptions(int deviceId)
        {
            return base.GetSubscriptions(deviceId, 0);
        }

        public IEnumerable<WebSocketConnectionBase> GetConnections(int deviceId)
        {
            return base.GetConnections(deviceId, 0);
        }

        private int GetKey(int? deviceId)
        {
            return deviceId ?? 0;
        }
    }
}