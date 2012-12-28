using System.Collections.Generic;
using DeviceHive.WebSockets.Network;

namespace DeviceHive.WebSockets.Subscriptions
{
    public class DeviceSubscriptionManager : SubscriptionManager<int>
    {
        public DeviceSubscriptionManager() : base("DeviceSubscriptions")
        {
        }

        public void Subscribe(WebSocketConnectionBase connection, int? deviceId)
        {
            base.Subscribe(connection, GetKey(deviceId));
        }

        public void Unsubscribe(WebSocketConnectionBase connection, int? deviceId)
        {
            base.Unsubscribe(connection, GetKey(deviceId));
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