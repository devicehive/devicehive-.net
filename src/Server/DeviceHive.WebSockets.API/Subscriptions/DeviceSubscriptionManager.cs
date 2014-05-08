using DeviceHive.WebSockets.Core.Network;
using System;
using System.Collections.Generic;

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

        public Subscription<int> Subscribe(Guid subscriptionId, WebSocketConnectionBase connection, int[] deviceIds, string[] names)
        {
            return base.Subscribe(subscriptionId, connection, deviceIds ?? new int[] { 0 }, names);
        }

        public Subscription<int> Subscribe(WebSocketConnectionBase connection, int deviceId)
        {
            return base.Subscribe(Guid.NewGuid(), connection, new int[] { deviceId }, null);
        }

        public IEnumerable<Subscription<int>> GetSubscriptions(int deviceId)
        {
            return base.GetSubscriptions(deviceId, 0);
        }
    }
}