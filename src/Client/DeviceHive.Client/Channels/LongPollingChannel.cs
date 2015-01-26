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
        private readonly RestClient _restClient;
        private readonly Dictionary<Guid, SubscriptionTask> _subscriptionTasks = new Dictionary<Guid, SubscriptionTask>();

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="connectionInfo">DeviceHive connection information.</param>
        public LongPollingChannel(DeviceHiveConnectionInfo connectionInfo)
            : base(connectionInfo)
        {
            _restClient = new RestClient(connectionInfo);
        }
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
            SetChannelState(ChannelState.Connected);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Closes the persistent connection to the DeviceHive server.
        /// </summary>
        /// <returns></returns>
        public override async Task CloseAsync()
        {
            SetChannelState(ChannelState.Disconnected); // that clears all subscriptions

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

            var result = await _restClient.PostAsync(string.Format("device/{0}/notification", deviceGuid), notification);
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
        /// <param name="token">Cancellation token to cancel polling command result.</param>
        /// <returns>Sent Command object.</returns>
        public override async Task<Command> SendCommandAsync(string deviceGuid, Command command, Action<Command> callback = null, CancellationToken? token = null)
        {
            if (string.IsNullOrEmpty(deviceGuid))
                throw new ArgumentException("DeviceGuid is null or empty!", "deviceGuid");
            if (command == null)
                throw new ArgumentNullException("command");
            if (!token.HasValue)
                token = CancellationToken.None;

            var result = await _restClient.PostAsync(string.Format("device/{0}/command", deviceGuid), command);
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
            await _restClient.PutAsync(string.Format("device/{0}/command/{1}", deviceGuid, command.Id), update);
        }
        #endregion

        #region Protected Methods

        /// <summary>
        /// Invoked after new subscription is added.
        /// The method starts a polling thread.
        /// </summary>
        /// <param name="subscription">A <see cref="ISubscription"/> object representing a subscription.</param>
        /// <returns></returns>
        protected override Task SubscriptionAdded(ISubscription subscription)
        {
            var subscriptionTask = new SubscriptionTask(subscription);
            var cancellationToken = subscriptionTask.CancellationTokenSource.Token;

            switch (subscription.Type)
            {
                case SubscriptionType.Notification:
                    subscriptionTask.Run(async () => await PollNotificationTaskMethodAsync(subscription, cancellationToken));
                    break;
                case SubscriptionType.Command:
                    subscriptionTask.Run(async () => await PollCommandTaskMethodAsync(subscription, cancellationToken));
                    break;
            }

            lock (_subscriptionTasks)
            {
                _subscriptionTasks[subscription.Id] = subscriptionTask;
            }

            return base.SubscriptionAdded(subscription);
        }

        /// <summary>
        /// Invoked after an existing subscription is removed.
        /// The method cancels the polling threads.
        /// </summary>
        /// <param name="subscription">A <see cref="ISubscription"/> object representing a subscription.</param>
        /// <returns></returns>
        protected override Task SubscriptionRemoved(ISubscription subscription)
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

            return base.SubscriptionRemoved(subscription);
        }
        #endregion

        #region Private Methods

        private async Task PollNotificationTaskMethodAsync(ISubscription subscription, CancellationToken cancellationToken)
        {
            var apiInfo = await _restClient.GetAsync<ApiInfo>("info");
            var timestamp = apiInfo.ServerTimestamp;

            while (true)
            {
                try
                {
                    var notifications = await PollNotificationsAsync(subscription.DeviceGuids, subscription.EventNames, timestamp, cancellationToken);
                    foreach (var notification in notifications)
                    {
                        notification.SubscriptionId = subscription.Id;
                        InvokeSubscriptionCallback(notification);
                    }

                    timestamp = notifications.Max(n => n.Notification.Timestamp ?? timestamp);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception)
                {
                    Task.Delay(1000).Wait(); // retry with small wait
                }
            }
        }

        private async Task PollCommandTaskMethodAsync(ISubscription subscription, CancellationToken cancellationToken)
        {
            var apiInfo = await _restClient.GetAsync<ApiInfo>("info", cancellationToken);
            var timestamp = apiInfo.ServerTimestamp;

            while (true)
            {
                try
                {
                    var commands = await PollCommandsAsync(subscription.DeviceGuids, subscription.EventNames, timestamp, cancellationToken);
                    foreach (var command in commands)
                    {
                        command.SubscriptionId = subscription.Id;
                        InvokeSubscriptionCallback(command);
                    }

                    timestamp = commands.Max(n => n.Command.Timestamp ?? timestamp);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception)
                {
                    Task.Delay(1000).Wait(); // retry with small wait
                }
            }
        }

        private async Task<List<DeviceNotification>> PollNotificationsAsync(string[] deviceGuids, string[] names, DateTime? timestamp, CancellationToken token)
        {
            var url = "device/notification/poll";
            var parameters = new[]
                {
                    timestamp == null ? null : "timestamp=" + timestamp.Value.ToString("yyyy-MM-ddTHH:mm:ss.ffffff"),
                    deviceGuids == null ? null : "deviceGuids=" + (string.Join(",", deviceGuids)),
                    names == null ? null : "names=" + (string.Join(",", names)),
                }.Where(p => p != null);
            if (parameters.Any())
                url += "?" + string.Join("&", parameters);

            while (true)
            {
                var notifications = await _restClient.GetAsync<List<DeviceNotification>>(url, token);
                if (notifications != null && notifications.Any())
                    return notifications;
            }
        }

        private async Task<List<DeviceCommand>> PollCommandsAsync(string[] deviceGuids, string[] names, DateTime? timestamp, CancellationToken token)
        {
            var url = "device/command/poll";
            var parameters = new[]
                {
                    timestamp == null ? null : "timestamp=" + timestamp.Value.ToString("yyyy-MM-ddTHH:mm:ss.ffffff"),
                    deviceGuids == null ? null : "deviceGuids=" + (string.Join(",", deviceGuids)),
                    names == null ? null : "names=" + (string.Join(",", names)),
                }.Where(p => p != null);
            if (parameters.Any())
                url += "?" + string.Join("&", parameters);

            while (true)
            {
                var commands = await _restClient.GetAsync<List<DeviceCommand>>(url, token);
                if (commands != null && commands.Any())
                    return commands;
            }
        }

        private async Task<Command> PollCommandUpdateAsync(string deviceGuid, int commandId, CancellationToken token)
        {
            return await _restClient.GetAsync<Command>(string.Format("device/{0}/command/{1}/poll", deviceGuid, commandId), token);
        }
        #endregion

        #region SubscriptionTask class

        private class SubscriptionTask
        {
            public ISubscription Subscription { get; private set; }
            public CancellationTokenSource CancellationTokenSource { get; private set; }
            public Task Task { get; private set; }

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
