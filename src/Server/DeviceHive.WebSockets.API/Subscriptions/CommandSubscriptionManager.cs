using DeviceHive.WebSockets.Core.Network;
using System;

namespace DeviceHive.WebSockets.API.Subscriptions
{
    public class CommandSubscriptionManager: SubscriptionManager<int>
    {
        public CommandSubscriptionManager()
            : base("CommandSubscriptions")
        {
        }

        public Subscription<int> Subscribe(WebSocketConnectionBase connection, int commandId)
        {
            return base.Subscribe(Guid.NewGuid(), connection, new int[] { commandId }, null);
        }
    }
}