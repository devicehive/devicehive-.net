using System;
using System.Collections.Generic;
using DeviceHive.WebSockets.Network;

namespace DeviceHive.WebSockets.Subscriptions
{
    public class DeviceSubscriptionManager : SubscriptionManager<Guid>
    {
        public DeviceSubscriptionManager() : base("DeviceSubscriptions")
        {
        }

        public void Subscribe(WebSocketConnectionBase connection, Guid? deviceGuid)
        {
            base.Subscribe(connection, GetKey(deviceGuid));
        }

        public void Unsubscribe(WebSocketConnectionBase connection, Guid? deviceGuid)
        {
            base.Unsubscribe(connection, GetKey(deviceGuid));
        }

        public IEnumerable<WebSocketConnectionBase> GetConnections(Guid deviceGuid)
        {
            return base.GetConnections(deviceGuid, Guid.Empty);
        }

        private Guid GetKey(Guid? deviceGuid)
        {
            return deviceGuid ?? Guid.Empty;
        }
    }
}