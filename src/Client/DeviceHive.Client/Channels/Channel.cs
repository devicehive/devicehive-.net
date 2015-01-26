using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceHive.Client
{
    /// <summary>
    /// The base class for channels that represent a persistent connection to the DeviceHive server.
    /// </summary>
    public abstract class Channel : IDisposable
    {
        private ApiInfo _apiInfo;
        private readonly List<Subscription> _subscriptions = new List<Subscription>();
        private readonly Dictionary<int, CommandCallback> _commandCallbacks = new Dictionary<int, CommandCallback>();

        #region Public Properties

        /// <summary>
        /// Gets current channel state.
        /// </summary>
        public ChannelState State { get; private set; }

        #endregion

        #region Public Events

        /// <summary>
        /// Fires when a channel state changes.
        /// </summary>
        public event EventHandler<ChannelStateEventArgs> StateChanged;

        #endregion

        #region Protected Properties

        /// <summary>
        /// Gets DeviceHive connection information.
        /// </summary>
        protected DeviceHiveConnectionInfo ConnectionInfo { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="connectionInfo">DeviceHive connection information.</param>
        protected Channel(DeviceHiveConnectionInfo connectionInfo)
        {
            if (connectionInfo == null)
                throw new ArgumentNullException("connectionInfo");

            State = ChannelState.Disconnected;
            ConnectionInfo = connectionInfo;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if current channel object can be used to eshtablish connection to the DeviceHive server.
        /// </summary>
        /// <returns>True if connection can be eshtablished.</returns>
        public abstract Task<bool> CanConnectAsync();

        /// <summary>
        /// Opens a persistent connection to the DeviceHive server.
        /// </summary>
        /// <returns></returns>
        public abstract Task OpenAsync();

        /// <summary>
        /// Closes the persistent connection to the DeviceHive server.
        /// </summary>
        /// <returns></returns>
        public abstract Task CloseAsync();

        /// <summary>
        /// Gets active subscriptions for DeviceHive commands and notifications.
        /// </summary>
        /// <returns>A list of <see cref="ISubscription"/> objects representing subsription information.</returns>
        public IList<ISubscription> GetSubscriptions()
        {
            lock (_subscriptions)
            {
                return _subscriptions.Cast<ISubscription>().ToList();
            }
        }

        /// <summary>
        /// Adds a subscription to device notifications.
        /// Notifications could be sent by devices or by clients on behalf of devices.
        /// </summary>
        /// <param name="deviceGuids">Array of device unique identifiers to subscribe to. Specify null to subscribe to all accessible devices.</param>
        /// <param name="notificationNames">Array of notification names to subsribe to. Specify null to subscribe to all notifications.</param>
        /// <param name="callback">A callback which will be invoken when a notification is retrieved.</param>
        /// <returns>An <see cref="ISubscription"/> object representing the subscription created.</returns>
        public async Task<ISubscription> AddNotificationSubscriptionAsync(string[] deviceGuids, string[] notificationNames, Action<DeviceNotification> callback)
        {
            CheckConnection();

            Action<object> notificationCallback = obj => callback((DeviceNotification)obj);
            var subscription = new Subscription(SubscriptionType.Notification, deviceGuids, notificationNames, notificationCallback);
            
            subscription.Id = await SubscriptionAdding(subscription);
            lock (_subscriptions)
            {
                _subscriptions.Add(subscription);
            }
            await SubscriptionAdded(subscription);

            return subscription;
        }

        /// <summary>
        /// Adds a subscription to device commands.
        /// Commands could only be sent by clients; this subscription would allow to listen to all commands sent to devices.
        /// </summary>
        /// <param name="deviceGuids">Array of device unique identifiers to subscribe to. Specify null to subscribe to all accessible devices.</param>
        /// <param name="commandNames">Array of command names to subsribe to. Specify null to subscribe to all commands.</param>
        /// <param name="callback">A callback which will be invoken when a command is retrieved.</param>
        /// <returns>An <see cref="ISubscription"/> object representing the subscription created.</returns>
        public async Task<ISubscription> AddCommandSubscriptionAsync(string[] deviceGuids, string[] commandNames, Action<DeviceCommand> callback)
        {
            CheckConnection();

            Action<object> commandCallback = obj => callback((DeviceCommand)obj);
            var subscription = new Subscription(SubscriptionType.Command, deviceGuids, commandNames, commandCallback);

            subscription.Id = await SubscriptionAdding(subscription);
            lock (_subscriptions)
            {
                _subscriptions.Add(subscription);
            }
            await SubscriptionAdded(subscription);

            return subscription;
        }

        /// <summary>
        /// Removes an existing subcription.
        /// The method does not throw an exception if subscription has already been removed.
        /// </summary>
        /// <param name="subscription">An <see cref="ISubscription"/> object representing the subscription to remove.</param>
        /// <returns></returns>
        public async Task RemoveSubscriptionAsync(ISubscription subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException("subscription");

            CheckConnection();

            var subscriptionObject = _subscriptions.FirstOrDefault(s => object.ReferenceEquals(s, subscription));
            if (subscriptionObject == null)
                return; // invalid subscription object passed

            await SubscriptionRemoving(subscription);
            lock (_subscriptions)
            {
                _subscriptions.Remove(subscriptionObject);
            }
            await SubscriptionRemoved(subscription);
        }

        /// <summary>
        /// Sends a notification on behalf of device.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="notification">A <see cref="Notification"/> object to be sent.</param>
        /// <returns>Sent Notification object.</returns>
        public abstract Task<Notification> SendNotificationAsync(string deviceGuid, Notification notification);

        /// <summary>
        /// Sends a command to the device.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="command">A <see cref="Command"/> object to be sent.</param>
        /// <param name="callback">A callback action to invoke when the command is completed by the device.</param>
        /// <param name="token">Cancellation token to cancel polling command result.</param>
        /// <returns>Sent Command object.</returns>
        public abstract Task<Command> SendCommandAsync(string deviceGuid, Command command, Action<Command> callback = null, CancellationToken? token = null);

        /// <summary>
        /// Updates a command on behalf of the device.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="command">A <see cref="Command"/> object to update.</param>
        public abstract Task UpdateCommandAsync(string deviceGuid, Command command);

        #endregion

        #region Protected Methods

        /// <summary>
        /// Gets DeviceHive API information.
        /// </summary>
        /// <returns>The <see cref="ApiInfo"/> object.</returns>
        protected async Task<ApiInfo> GetApiInfoAsync()
        {
            if (_apiInfo == null)
            {
                var restClient = new RestClient(ConnectionInfo);
                _apiInfo = await restClient.GetAsync<ApiInfo>("info");
            }
            return _apiInfo;
        }

        /// <summary>
        /// Checks if connection is open.
        /// Otherwise throws an InvalidOperationException exception.
        /// </summary>
        protected void CheckConnection()
        {
            if (State != ChannelState.Connected)
                throw new InvalidOperationException("The channel is not opened, please call the Open method and wait until it completes!");
        }

        /// <summary>
        /// Sets new channel state.
        /// The method updates the <see cref="State"/> property and invokes the <see cref="OnChannelStateChanged"/> method.
        /// </summary>
        /// <param name="state">The new <see cref="ChannelState"/> value.</param>
        protected void SetChannelState(ChannelState state)
        {
            if (state == ChannelState.Disconnected)
            {
                lock (_subscriptions)
                {
                    _subscriptions.Clear();
                }
            }

            var eventArgs = new ChannelStateEventArgs(State, state);
            State = state;

            Task.Run(() => OnChannelStateChanged(eventArgs));
        }

        /// <summary>
        /// Fires the <see cref="StateChanged"/> event.
        /// </summary>
        /// <param name="eventArgs">The <see cref="ChannelStateEventArgs"/> object.</param>
        protected virtual void OnChannelStateChanged(ChannelStateEventArgs eventArgs)
        {
            if (eventArgs == null)
                throw new ArgumentNullException("eventArgs");

            if (StateChanged != null)
                StateChanged(this, eventArgs);
        }

        /// <summary>
        /// Invoked before new subscription is added.
        /// The implementer must apply necessary behavior to notify the server about new subscription.
        /// </summary>
        /// <param name="subscription">A <see cref="ISubscription"/> object representing a subscription.</param>
        /// <returns>Subscription unique identifier.</returns>
        protected virtual Task<Guid> SubscriptionAdding(ISubscription subscription)
        {
            return Task.FromResult(Guid.NewGuid());
        }

        /// <summary>
        /// Invoked after new subscription is added.
        /// The implementer must apply necessary behavior to notify the server about new subscription.
        /// </summary>
        /// <param name="subscription">A <see cref="ISubscription"/> object representing a subscription.</param>
        /// <returns></returns>
        protected virtual Task SubscriptionAdded(ISubscription subscription)
        {
            return Task.FromResult(false);
        }

        /// <summary>
        /// Invoked before an existing subscription is removed.
        /// The implementer must apply necessary behavior to notify the server about subscription removal.
        /// </summary>
        /// <param name="subscription">A <see cref="ISubscription"/> object representing a subscription.</param>
        /// <returns></returns>
        protected virtual Task SubscriptionRemoving(ISubscription subscription)
        {
            return Task.FromResult(false);
        }

        /// <summary>
        /// Invoked after an existing subscription is removed.
        /// The implementer must apply necessary behavior to notify the server about subscription removal.
        /// </summary>
        /// <param name="subscription">A <see cref="ISubscription"/> object representing a subscription.</param>
        /// <returns></returns>
        protected virtual Task SubscriptionRemoved(ISubscription subscription)
        {
            return Task.FromResult(false);
        }

        /// <summary>
        /// The methods invokes a subscription callback for new notification received from the DeviceHive server.
        /// </summary>
        /// <param name="notification">A <see cref="DeviceNotification"/> object received from the DeviceHive server.</param>
        protected void InvokeSubscriptionCallback(DeviceNotification notification)
        {
            if (notification == null)
                throw new ArgumentNullException("notification");

            Subscription subscription;
            lock (_subscriptions)
            {
                subscription = _subscriptions.FirstOrDefault(s => s.Id == notification.SubscriptionId);
            }
            if (subscription != null)
            {
                Task.Run(() =>
                {
                    try { subscription.Callback(notification); }
                    catch (Exception) { }
                });
            }
        }

        /// <summary>
        /// The methods invokes a subscription callback for new command received from the DeviceHive server.
        /// </summary>
        /// <param name="command">A <see cref="DeviceCommand"/> object received from the DeviceHive server.</param>
        protected void InvokeSubscriptionCallback(DeviceCommand command)
        {
            if (command == null)
                throw new ArgumentNullException("command");

            Subscription subscription;
            lock (_subscriptions)
            {
                subscription = _subscriptions.FirstOrDefault(s => s.Id == command.SubscriptionId);
            }
            if (subscription != null)
            {
                Task.Run(() =>
                {
                    try { subscription.Callback(command); }
                    catch (Exception) { }
                });
            }
        }

        /// <summary>
        /// The method registers a callback to be fired when the <see cref="InvokeCommandCallback"/> method with matching command ID is invoked.
        /// </summary>
        /// <param name="commandId">Command identifier.</param>
        /// <param name="callback">Callback method to invoke.</param>
        protected void RegisterCommandCallback(int commandId, Action<Command> callback)
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            lock (_commandCallbacks)
            {
                CommandCallback commandCallback;
                if (_commandCallbacks.TryGetValue(commandId, out commandCallback))
                {
                    // InvokeCommandCallback has already been invoked and it's waiting for the action delegate
                    commandCallback.SetCallback(callback);
                }
                else
                {
                    // store callback for future invocation
                    commandCallback = new CommandCallback(callback);
                    _commandCallbacks[commandId] = commandCallback;
                }
            }
        }

        /// <summary>
        /// Invokes a callback previously registered using the <see cref="RegisterCommandCallback"/> method with matching command ID.
        /// </summary>
        /// <param name="command">A <see cref="Command"/> object used as argument to the callback.</param>
        protected void InvokeCommandCallback(Command command)
        {
            if (command == null)
                throw new ArgumentNullException("command");
            if (command.Id == null)
                throw new ArgumentException("command.Id is null", "command");

            var commandId = command.Id.Value;

            // retrieve a callback object
            CommandCallback commandCallback;
            lock (_commandCallbacks)
            {
                if (!_commandCallbacks.TryGetValue(commandId, out commandCallback))
                {
                    // callback has not been registered yet: create an empty callback with a wait handle
                    commandCallback = new CommandCallback();
                    _commandCallbacks[commandId] = commandCallback;
                }
            }

            // if there's not callback delegate - wait for it
            if (commandCallback.Callback == null && commandCallback.WaitHandle != null)
            {
                Task.WhenAny(commandCallback.WaitHandle.Task, Task.Delay(10000)).Wait();
            }

            // if a callback now exists - invoke it in a separate task
            if (commandCallback.Callback != null)
            {
                Task.Run(() =>
                {
                    try { commandCallback.Callback(command); }
                    catch (Exception) { }
                });
            }

            // clean up
            lock (_commandCallbacks)
            {
                _commandCallbacks.Remove(commandId);
            }
        }

        /// <summary>
        /// Serializes passed object to JSON.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="obj">Object to serialize.</param>
        /// <returns>The <see cref="JObject"/> object.</returns>
        protected static JObject Serialize<T>(T obj)
            where T : class
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            var serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.ContractResolver = new JsonContractResolver();
            return JObject.FromObject(obj, serializer);
        }

        /// <summary>
        /// Deserializes object from JSON.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="json">A <see cref="JObject"/> object representing JSON.</param>
        /// <returns>Deserialized object.</returns>
        protected static T Deserialize<T>(JObject json)
        {
            if (json == null)
                throw new ArgumentNullException("json");

            var serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.ContractResolver = new JsonContractResolver();
            return json.ToObject<T>(serializer);
        }
        #endregion

        #region IDisposable Members

        /// <summary>
        /// Disposes current object.
        /// </summary>
        public virtual void Dispose()
        {
            var task = CloseAsync();
            // does not need to wait
        }
        #endregion
    }
}
