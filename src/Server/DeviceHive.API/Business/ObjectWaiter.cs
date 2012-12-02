using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DeviceHive.API.Business
{
    /// <summary>
    /// Represents a primitive that allows to wait for object changes.
    /// Call <see cref="BeginWait"/> to start waiting for object changes.
    /// Call <see cref="NotifyChanges"/> from to notify all waiting threads about object change.
    /// </summary>
    public class ObjectWaiter
    {
        private readonly HashSet<Subscription> _subscriptions = new HashSet<Subscription>();

        #region Public Methods

        /// <summary>
        /// Begins waiting for objects changes with specified keys
        /// </summary>
        /// <param name="keys">Array of object keys to wait</param>
        /// <returns>IWaiterHandle interface</returns>
        public IWaiterHandle BeginWait(params object[] keys)
        {
            var subscription = Subscribe(keys);
            return new WaiterHandle(this, subscription);
        }

        /// <summary>
        /// Notifies about object change
        /// </summary>
        /// <param name="key">Object key</param>
        public void NotifyChanges(object key)
        {
            foreach (var subscription in GetSubscriptionsFor(key))
                subscription.Notify();
        }
        #endregion

        #region Private Methods

        private Subscription Subscribe(object[] keys)
        {
            lock (_subscriptions)
            {
                var subscription = new Subscription(keys);
                _subscriptions.Add(subscription);
                return subscription;
            }
        }

        private void Unsubscribe(Subscription subscription)
        {
            lock (_subscriptions)
            {
                _subscriptions.Remove(subscription);
            }
        }

        private Subscription[] GetSubscriptionsFor(object key)
        {
            lock (_subscriptions)
            {
                return _subscriptions.Where(s => s.Keys.Contains(key) || s.Keys.Contains(null)).ToArray();
            }
        }
        #endregion

        #region Subscription class

        private class Subscription
        {
            private readonly HashSet<object> _keys;
            private readonly ManualResetEvent _handle;

            public HashSet<object> Keys
            {
                get { return _keys; }
            }

            public WaitHandle Handle
            {
                get { return _handle; }
            }

            public Subscription(object[] keys)
            {
                _keys = new HashSet<object>(keys);
                _handle = new ManualResetEvent(false);
            }

            public void Notify()
            {
                _handle.Set();
            }
        }
        #endregion

        #region WaiterHandle class

        private class WaiterHandle : IWaiterHandle
        {
            private readonly ObjectWaiter _waiter;
            private readonly Subscription _subscription;

            public WaiterHandle(ObjectWaiter waiter, Subscription subscription)
            {
                _waiter = waiter;
                _subscription = subscription;
            }

            public WaitHandle Handle
            {
                get { return _subscription.Handle; }
            }

            public void Dispose()
            {
                _waiter.Unsubscribe(_subscription);
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents waiting handle interface
    /// </summary>
    public interface IWaiterHandle : IDisposable
    {
        /// <summary>
        /// Gets associated wait handle
        /// </summary>
        WaitHandle Handle { get; }
    }
}