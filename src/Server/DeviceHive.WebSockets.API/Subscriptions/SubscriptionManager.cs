using DeviceHive.WebSockets.Core.Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DeviceHive.WebSockets.API.Subscriptions
{
    public abstract class SubscriptionManager<TKey>
    {
        private readonly string _subscriptionsValueKey;

        private readonly ConcurrentDictionary<TKey, List<Subscription<TKey>>> _lookupByKey = new ConcurrentDictionary<TKey, List<Subscription<TKey>>>();
        private readonly ConcurrentDictionary<Guid, Subscription<TKey>> _lookupById = new ConcurrentDictionary<Guid, Subscription<TKey>>();

        protected SubscriptionManager(string subscriptionsValueKey)
        {
            _subscriptionsValueKey = subscriptionsValueKey;
        }

        public Subscription<TKey> Subscribe(Guid subscriptionId, WebSocketConnectionBase connection, TKey[] keys, object data)
        {
            var subscription = new Subscription<TKey>(subscriptionId, connection, keys, data);
             
            // add into connection session
            var connectionSubscriptions = GetConnectionSubscriptions(connection);
            connectionSubscriptions.Add(subscription);

            // add into lookup by key
            foreach (var key in subscription.Keys)
            {
                var subscriptionList = _lookupByKey.GetOrAdd(key, new List<Subscription<TKey>>());
                subscriptionList.Add(subscription);
            }

            // add into lookup by id
            _lookupById[subscription.Id] = subscription;
            return subscription;
        }

        public void Unsubscribe(WebSocketConnectionBase connection, Guid subscriptionId)
        {
            // remove from lookup by id
            Subscription<TKey> subscription;
            _lookupById.TryRemove(subscriptionId, out subscription);

            if (subscription != null)
            {
                // remove from connection session
                var connectionSubscriptions = GetConnectionSubscriptions(connection);
                connectionSubscriptions.Remove(subscription);

                // remove from subscription list
                List<Subscription<TKey>> subscriptionList;
                foreach (var key in subscription.Keys)
                {
                    if (_lookupByKey.TryGetValue(key, out subscriptionList))
                        subscriptionList.Remove(subscription);
                }
            }
        }

        public IEnumerable<Subscription<TKey>> GetSubscriptions(params TKey[] keys)
        {
            return keys.SelectMany(key => {
                List<Subscription<TKey>> subscriptionList;
                return _lookupByKey.TryGetValue(key, out subscriptionList) ? subscriptionList : new List<Subscription<TKey>>(0);
            }).ToArray();
        }

        public IEnumerable<Subscription<TKey>> GetSubscriptions(WebSocketConnectionBase connection)
        {
            return GetConnectionSubscriptions(connection).ToArray();
        }

        public void Cleanup(WebSocketConnectionBase connection)
        {
            // get subscription from session
            var connectionSubscriptions = GetConnectionSubscriptions(connection);

            foreach (var subscription in connectionSubscriptions)
            {
                // remove from lookup by id
                Subscription<TKey> s;
                _lookupById.TryRemove(subscription.Id, out s);

                // remove from subscription list
                List<Subscription<TKey>> subscriptionList;
                foreach (var key in subscription.Keys)
                {
                    if (_lookupByKey.TryGetValue(key, out subscriptionList))
                        subscriptionList.Remove(subscription);
                }
            }
        }

        private List<Subscription<TKey>> GetConnectionSubscriptions(WebSocketConnectionBase connection)
        {
            return (List<Subscription<TKey>>) connection.Session.GetOrAdd(
                _subscriptionsValueKey, () => new List<Subscription<TKey>>());
        }
    }
}