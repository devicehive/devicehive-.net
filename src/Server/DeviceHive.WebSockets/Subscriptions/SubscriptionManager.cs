using System.Collections.Generic;
using System.Linq;
using DeviceHive.WebSockets.Network;

namespace DeviceHive.WebSockets.Subscriptions
{
    public abstract class SubscriptionManager<TKey>
    {
        private readonly string _subscriptionsValueKey;

        private readonly SubscriptionCollection<TKey> _subscriptionCollection =
            new SubscriptionCollection<TKey>();


        protected SubscriptionManager(string subscriptionsValueKey)
        {
            _subscriptionsValueKey = subscriptionsValueKey;
        }


        public void Subscribe(WebSocketConnectionBase connection, TKey key)
        {
            var subscription = new Subscription<TKey>(key, connection);
             
            var connectionSubscriptions = GetSubscriptions(connection);
                connectionSubscriptions.Add(subscription);

            var subscriptionList = _subscriptionCollection.GetSubscriptionList(key);
            subscriptionList.Add(subscription);
        }

        public void Unsubscribe(WebSocketConnectionBase connection, TKey key)
        {
            var connectionSubscriptions = GetSubscriptions(connection);
            connectionSubscriptions.RemoveAll(s => object.Equals(s.Key, key));

            var subscriptionList = _subscriptionCollection.GetSubscriptionList(key);
            subscriptionList.RemoveAll(s => s.Connection == connection);
        }

        public IEnumerable<WebSocketConnectionBase> GetConnections(params TKey[] keys)
        {
            return keys
                .SelectMany(k => _subscriptionCollection.GetSubscriptionList(k))
                .Select(s => s.Connection)
                .Distinct()
                .ToArray();
        }

        public void Cleanup(WebSocketConnectionBase connection)
        {
            var deviceGuids = GetSubscriptions(connection).Select(s => s.Key).Distinct().ToArray();

            foreach (var deviceGuid in deviceGuids)
            {
                var subscriptionList = _subscriptionCollection.GetSubscriptionList(deviceGuid);
                subscriptionList.RemoveAll(s => s.Connection == connection);
            }
        }


        private List<Subscription<TKey>> GetSubscriptions(WebSocketConnectionBase connection)
        {
            return (List<Subscription<TKey>>) connection.Session.GetOrAdd(
                _subscriptionsValueKey, () => new List<Subscription<TKey>>());
        }


        #region Inner classes

        private class SubscriptionCollection<TKey>
        {
            private readonly object _lock = new object();

            private readonly Dictionary<TKey, SubscriptionList<TKey>> _subscriptions =
                new Dictionary<TKey, SubscriptionList<TKey>>();

            public SubscriptionList<TKey> GetSubscriptionList(TKey key)
            {
                lock (_lock)
                {
                    SubscriptionList<TKey> list;
                    if (!_subscriptions.TryGetValue(key, out list))
                    {
                        list = new SubscriptionList<TKey>();
                        _subscriptions.Add(key, list);
                    }

                    return list;
                }
            }
        }

        private class SubscriptionList<TKey> : List<Subscription<TKey>>
        {            
        }

        #endregion
    }
}