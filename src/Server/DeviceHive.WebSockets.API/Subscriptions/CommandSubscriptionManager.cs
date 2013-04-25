namespace DeviceHive.WebSockets.API.Subscriptions
{
    public class CommandSubscriptionManager: SubscriptionManager<int>
    {
        public CommandSubscriptionManager() : base("CommandSubscriptions")
        {
        }
    }
}