using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents implementation of the persistent connection to the DeviceHive server using long-polling mechanism.
    /// </summary>
    public class LongPollingChannel : Channel
    {
        private readonly Dictionary<Guid, SubscriptionTask> _subscriptionTasks = new Dictionary<Guid, SubscriptionTask>();

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="connectionInfo">DeviceHive connection information.</param>
        public LongPollingChannel(DeviceHiveConnectionInfo connectionInfo)
            : this(connectionInfo, null)
        {
        }
        
        /// <summary>
        /// Constructor which allows to override <see cref="IRestClient" /> which makes HTTP requests to the DeviceHive server.
        /// </summary>
        /// <param name="connectionInfo">DeviceHive connection information.</param>
        /// <param name="restClient">IRestClient implementation.</param>
        public LongPollingChannel(DeviceHiveConnectionInfo connectionInfo, IRestClient restClient)
            : base(connectionInfo, restClient)
        {
            CommandUpdatePollTimeout = TimeSpan.FromSeconds(30);
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets default command poll timeout to apply in the <see cref="SendCommandAsync"/> method.
        /// Please avoid using too high value, as waiting command results is occupying a HTTP connection.
        /// Default value is 30 seconds.
        /// </summary>
        public TimeSpan CommandUpdatePollTimeout { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if current channel object can be used to eshtablish connection to the DeviceHive server.
        /// </summary>
        /// <returns>True if connection can be eshtablished.</returns>
        public override Task<bool> CanConnectAsync()
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Opens a persistent connection to the DeviceHive server.
        /// </summary>
        /// <returns></returns>
        public override Task OpenAsync()
        {
            if (State != ChannelState.Disconnected)
                throw new InvalidOperationException("The connection is already open, please call the CloseAsync method before opening it again!");

            SetChannelState(ChannelState.Connected);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Closes the persistent connection to the DeviceHive server.
        /// </summary>
        /// <returns></returns>
        public override async Task CloseAsync()
        {
            SubscriptionTask[] subscriptionTasks;
            lock (_subscriptionTasks)
            {
                subscriptionTasks = _subscriptionTasks.Values.ToArray();
                _subscriptionTasks.Clear();
            }

            foreach (var subscriptionTask in subscriptionTasks)
            {
                subscriptionTask.CancellationTokenSource.Cancel();
            }
            await Task.WhenAll(subscriptionTasks.Select(t => t.Task));

            SetChannelState(ChannelState.Disconnected); // that clears all subscriptions
        }

        /// <summary>
        /// Sends a notification on behalf of device.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="notification">A <see cref="Notification"/> object to be sent.</param>
        /// <returns>Sent Notification object.</returns>
        public override async Task<Notification> SendNotificationAsync(string deviceGuid, Notification notification)
        {
            if (string.IsNullOrEmpty(deviceGuid))
                throw new ArgumentException("DeviceGuid is null or empty!", "deviceGuid");
            if (notification == null)
                throw new ArgumentNullException("notification");

            var result = await RestClient.PostAsync(string.Format("device/{0}/notification", deviceGuid), notification);
            notification.Id = result.Id;
            notification.Timestamp = result.Timestamp;
            return notification;
        }

        /// <summary>
        /// Sends a command to the device.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="command">A <see cref="Command"/> object to be sent.</param>
        /// <param name="callback">A callback action to invoke when the command is completed by the device.</param>
        /// <param name="token">Cancellation token to cancel waiting for command result.</param>
        /// <returns>Sent Command object.</returns>
        public override async Task<Command> SendCommandAsync(string deviceGuid, Command command, Action<Command> callback = null, CancellationToken? token = null)
        {
            if (string.IsNullOrEmpty(deviceGuid))
                throw new ArgumentException("DeviceGuid is null or empty!", "deviceGuid");
            if (command == null)
                throw new ArgumentNullException("command");
            if (!token.HasValue)
                token = new CancellationTokenSource(CommandUpdatePollTimeout).Token;

            var result = await RestClient.PostAsync(string.Format("device/{0}/command", deviceGuid), command);
            command.Id = result.Id;
            command.Timestamp = result.Timestamp;
            command.UserId = result.UserId;

            if (callback != null)
            {
                var task = Task.Run(async () =>
                {
                    var update = await PollCommandUpdateAsync(deviceGuid, command.Id.Value, token.Value);
                    if (update != null)
                        callback(update);
                });
            }

            return command;
        }

        /// <summary>
        /// Updates a command on behalf of the device.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="command">A <see cref="Command"/> object to update.</param>
        public override async Task UpdateCommandAsync(string deviceGuid, Command command)
        {
            if (string.IsNullOrEmpty(deviceGuid))
                throw new ArgumentException("DeviceGuid is null or empty!", "deviceGuid");
            if (command == null)
                throw new ArgumentNullException("command");
            if (command.Id == null)
                throw new ArgumentException("Command ID is null!", "command");

            var update = new Command { Status = command.Status, Result = command.Result };
            await RestClient.PutAsync(string.Format("device/{0}/command/{1}", deviceGuid, command.Id), update);
        }
        #endregion

        #region Protected Methods

        /// <summary>
        /// Invoked after new subscription is added.
        /// The method starts a polling thread.
        /// </summary>
        /// <param name="subscription">A <see cref="ISubscription"/> object representing a subscription.</param>
        /// <returns></returns>
        protected override async Task SubscriptionAdded(ISubscription subscription)
        {
            var subscriptionTask = new SubscriptionTask(subscription);
            switch (subscription.Type)
            {
                case SubscriptionType.Notification:
                    subscriptionTask.Run(async () => await PollNotificationTaskMethodAsync(subscriptionTask));
                    break;
                case SubscriptionType.Command:
                    subscriptionTask.Run(async () => await PollCommandTaskMethodAsync(subscriptionTask));
                    break;
            }

            lock (_subscriptionTasks)
            {
                _subscriptionTasks[subscription.Id] = subscriptionTask;
            }

            await base.SubscriptionAdded(subscription);
        }

        /// <summary>
        /// Invoked after an existing subscription is removed.
        /// The method cancels the polling threads.
        /// </summary>
        /// <param name="subscription">A <see cref="ISubscription"/> object representing a subscription.</param>
        /// <returns></returns>
        protected override async Task SubscriptionRemoved(ISubscription subscription)
        {
            SubscriptionTask subscriptionTask;
            lock (_subscriptionTasks)
            {
                _subscriptionTasks.TryGetValue(subscription.Id, out subscriptionTask);
                if (subscriptionTask != null)
                    _subscriptionTasks.Remove(subscription.Id);
            }

            if (subscriptionTask != null)
            {
                subscriptionTask.CancellationTokenSource.Cancel();
                subscriptionTask.Task.Wait();
            }

            await base.SubscriptionRemoved(subscription);
        }
        #endregion

        #region Private Methods

        private async Task PollNotificationTaskMethodAsync(SubscriptionTask subscriptionTask)
        {
            var subscription = subscriptionTask.Subscription;
            var cancellationToken = subscriptionTask.CancellationTokenSource.Token;
            
            while (true)
            {
                try
                {
                    var notifications = await PollNotificationsAsync(subscription.DeviceGuids, subscription.EventNames,
                        subscription.Timestamp, subscriptionTask.IsLastPollFailed ? (int?)0 : null, cancellationToken);
                    NotifyPollResult(subscriptionTask, false);

                    foreach (var notification in notifications)
                    {
                        notification.SubscriptionId = subscription.Id;
                        InvokeSubscriptionCallback(subscription.Id, notification.Notification.Timestamp.Value, notification);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception)
                {
                    NotifyPollResult(subscriptionTask, true);
                    Task.Delay(1000).Wait(cancellationToken); // retry with small wait
                }
            }
        }

        private async Task PollCommandTaskMethodAsync(SubscriptionTask subscriptionTask)
        {
            var subscription = subscriptionTask.Subscription;
            var cancellationToken = subscriptionTask.CancellationTokenSource.Token;

            while (true)
            {
                try
                {
                    var commands = await PollCommandsAsync(subscription.DeviceGuids, subscription.EventNames,
                        subscription.Timestamp, subscriptionTask.IsLastPollFailed ? (int?)0 : null, cancellationToken);
                    NotifyPollResult(subscriptionTask, false);

                    foreach (var command in commands)
                    {
                        command.SubscriptionId = subscription.Id;
                        InvokeSubscriptionCallback(subscription.Id, command.Command.Timestamp.Value, command);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception)
                {
                    NotifyPollResult(subscriptionTask, true);
                    Task.Delay(1000).Wait(cancellationToken); // retry with small wait
                }
            }
        }

        private void NotifyPollResult(SubscriptionTask subscriptionTask, bool isFailed)
        {
            lock (_subscriptionTasks)
            {
                subscriptionTask.IsLastPollFailed = isFailed;

                if (State == ChannelState.Connected && isFailed)
                {
                    SetChannelState(ChannelState.Reconnecting);
                }
                else if (State == ChannelState.Reconnecting && !isFailed)
                {
                    if (!_subscriptionTasks.Values.Any(t => t.IsLastPollFailed))
                        SetChannelState(ChannelState.Connected);
                }
            }
        }

        private async Task<List<DeviceNotification>> PollNotificationsAsync(string[] deviceGuids, string[] names, DateTime? timestamp, int? waitTimeout, CancellationToken token)
        {
            var url = "device/notification/poll";
            var parameters = new[]
                {
                    timestamp == null ? null : "timestamp=" + timestamp.Value.ToString("yyyy-MM-ddTHH:mm:ss.ffffff"),
                    deviceGuids == null ? null : "deviceGuids=" + (string.Join(",", deviceGuids)),
                    names == null ? null : "names=" + (string.Join(",", names)),
                    waitTimeout == null ? null : "waitTimeout=" + waitTimeout,
                }.Where(p => p != null);
            if (parameters.Any())
                url += "?" + string.Join("&", parameters);

            return await RestClient.GetAsync<List<DeviceNotification>>(url, token);
        }

        private async Task<List<DeviceCommand>> PollCommandsAsync(string[] deviceGuids, string[] names, DateTime? timestamp, int? waitTimeout, CancellationToken token)
        {
            var url = "device/command/poll";
            var parameters = new[]
                {
                    timestamp == null ? null : "timestamp=" + timestamp.Value.ToString("yyyy-MM-ddTHH:mm:ss.ffffff"),
                    deviceGuids == null ? null : "deviceGuids=" + (string.Join(",", deviceGuids)),
                    names == null ? null : "names=" + (string.Join(",", names)),
                    waitTimeout == null ? null : "waitTimeout=" + waitTimeout,
                }.Where(p => p != null);
            if (parameters.Any())
                url += "?" + string.Join("&", parameters);

            return await RestClient.GetAsync<List<DeviceCommand>>(url, token);
        }

        private async Task<Command> PollCommandUpdateAsync(string deviceGuid, int commandId, CancellationToken token)
        {
            while (true)
            {
                var command = await RestClient.GetAsync<Command>(string.Format("device/{0}/command/{1}/poll", deviceGuid, commandId), token);
                if (command != null)
                    return command;
            }
        }
        #endregion

        #region SubscriptionTask class

        private class SubscriptionTask
        {
            public ISubscription Subscription { get; private set; }
            public CancellationTokenSource CancellationTokenSource { get; private set; }
            public Task Task { get; private set; }
            public bool IsLastPollFailed { get; set; }

            public SubscriptionTask(ISubscription subscription)
            {
                Subscription = subscription;
                CancellationTokenSource = new CancellationTokenSource();
            }

            public void Run(Action action)
            {
                Task = Task.Run(action, CancellationTokenSource.Token);
            }
        }
        #endregion
    }
}
