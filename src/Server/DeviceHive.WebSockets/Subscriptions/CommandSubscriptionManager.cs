namespace DeviceHive.WebSockets.Subscriptions
{
    public class CommandSubscriptionManager: SubscriptionManager<int>
    {
        public CommandSubscriptionManager() : base("CommandSubscriptions")
        {
        }
    }
}