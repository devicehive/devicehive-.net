using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace DeviceHive.Core.Messaging
{
    /// <summary>
    /// Represents a message bus used to pass messages between various internal components
    /// </summary>
    public abstract class MessageBus
    {
        private readonly SubscriptionStorage _subscriptions = new SubscriptionStorage();

        #region Public Methods

        /// <summary>
        /// Subscribes to a specific message type
        /// </summary>
        /// <typeparam name="TMessage">Message type</typeparam>
        /// <param name="handler">Message handler</param>
        public void Subscribe<TMessage>(Action<TMessage> handler) where TMessage : class 
        {
            _subscriptions[typeof(TMessage)].Add(msg => handler((TMessage) msg));
        }

        /// <summary>
        /// Notifies other listening clients about new message
        /// </summary>
        /// <typeparam name="TMessage">Message type</typeparam>
        /// <param name="message">Message object</param>
        public void Notify<TMessage>(TMessage message) where TMessage : class
        {
            var messageContainer = new MessageContainer()
            {
                TypeName = typeof (TMessage).FullName,
                Message = message
            };

            byte[] data;

            using (var ms = new MemoryStream())
            {
                var writer = new BsonWriter(ms);
                var serializer = new JsonSerializer();
                serializer.Serialize(writer, messageContainer);
                data = ms.ToArray();
            }

            SendMessage(data);
        }
        #endregion

        #region Protected Methods

        /// <summary>
        /// Handles an income message
        /// </summary>
        /// <param name="data">Message data</param>
        protected void HandleMessage(byte[] data)
        {
            MessageContainer messageContainer;

            using (var ms = new MemoryStream(data))
            {
                var reader = new BsonReader(ms);
                var serializer = new JsonSerializer();
                messageContainer = serializer.Deserialize<MessageContainer>(reader);
            }

            var handlers = _subscriptions[messageContainer.TypeName];
            foreach (var handler in handlers)
                handler(messageContainer.Message);
        }

        /// <summary>
        /// Sends message to all remote listeners
        /// </summary>
        /// <param name="data">Message data</param>
        protected abstract void SendMessage(byte[] data);

        #endregion

        #region SubscriptionList class

        private class SubscriptionList : List<Action<object>>
        {            
        }
        #endregion

        #region SubscriptionStorage class

        private class SubscriptionStorage
        {
            private readonly object _lock = new object();

            private readonly IDictionary<string, SubscriptionList> _lists =
                new Dictionary<string, SubscriptionList>();

            public SubscriptionList this[string typeFullName]
            {
                get
                {
                    lock (_lock)
                    {
                        SubscriptionList list;

                        if (!_lists.TryGetValue(typeFullName, out list))
                        {
                            list = new SubscriptionList();
                            _lists.Add(typeFullName, list);
                        }

                        return list;
                    }
                }
            }

            public SubscriptionList this[Type type]
            {
                get { return this[type.FullName]; }
            }
        }
        #endregion

        #region MessageContainer class

        private class MessageContainer
        {
            public string TypeName { get; set; }

            [JsonProperty(TypeNameHandling = TypeNameHandling.All)]
            public object Message { get; set; }
        }
        #endregion
    }
}