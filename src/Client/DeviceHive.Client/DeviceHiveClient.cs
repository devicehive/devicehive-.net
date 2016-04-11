using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents a client to the DeviceHive server.
    /// The client uses DeviceHive REST API for generic operations such as get networks, get devices, etc.
    /// and also utilizes one of available channels (LongPolling, WebSocket) for maintaining a persistent connection
    /// for retrieving real-time messages (notifications, commands) from the server according to subscriptions made.
    /// </summary>
    public class DeviceHiveClient : IDeviceHiveClient, IDisposable
    {
        private readonly AsyncLock _lock = new AsyncLock(); // synchronizes channel open/close operations
        private readonly IRestClient _restClient;

        private Channel[] _availableChannels;
        private Channel _channel;

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="connectionInfo">An instance of <see cref="DeviceHiveConnectionInfo" /> class which provides DeviceHive connection information.</param>
        public DeviceHiveClient(DeviceHiveConnectionInfo connectionInfo)
            : this(connectionInfo, null)
        {
        }

        /// <summary>
        /// Constructor which allows to override <see cref="IRestClient" /> which makes HTTP requests to the DeviceHive server.
        /// </summary>
        /// <param name="connectionInfo">An instance of <see cref="DeviceHiveConnectionInfo" /> class which provides DeviceHive connection information.</param>
        /// <param name="restClient">An instance of <see cref="IRestClient" /> which makes HTTP requests to the DeviceHive server.</param>
        public DeviceHiveClient(DeviceHiveConnectionInfo connectionInfo, IRestClient restClient)
        {
            if (connectionInfo == null)
                throw new ArgumentNullException("connectionInfo");

            _restClient = restClient ?? new RestClient(connectionInfo);
            
            // default channels: WebSocket, LongPolling
            SetAvailableChannels(new Channel[] {
#if !EXCLUDE_WEB_SOCKET
                new WebSocketChannel(connectionInfo, _restClient),
#endif
                new LongPollingChannel(connectionInfo, _restClient)
            });

#if !PORTABLE && !NETFX_CORE
            // allow at least 10 concurrent outbound connections
            if (ServicePointManager.DefaultConnectionLimit < 10)
                ServicePointManager.DefaultConnectionLimit = 10;
#endif
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Gets information about the currently logged-in user.
        /// </summary>
        /// <returns>The <see cref="User"/> object.</returns>
        public async Task<User> GetCurrentUserAsync()
        {
            return await _restClient.GetAsync<User>("user/current");
        }

        /// <summary>
        /// Updates the currently logged-in user.
        /// The method only updates the user password.
        /// </summary>
        /// <param name="user">The <see cref="User"/> object with the new password.</param>
        public async Task UpdateCurrentUserAsync(User user)
        {
            await _restClient.PutAsync<User>("user/current", user);
        }

        /// <summary>
        /// Gets a list of networks.
        /// </summary>
        /// <param name="filter">Network filter.</param>
        /// <returns>A list of <see cref="Network"/> objects that match specified filter criteria.</returns>
        public async Task<List<Network>> GetNetworksAsync(NetworkFilter filter = null)
        {
            return await _restClient.GetAsync<List<Network>>("network" + RestClient.MakeQueryString(filter));
        }

        /// <summary>
        /// Gets a list of devices.
        /// </summary>
        /// <param name="filter">Device filter criteria.</param>
        /// <returns>A list of <see cref="Device"/> objects that match specified filter criteria.</returns>
        public async Task<List<Device>> GetDevicesAsync(DeviceFilter filter = null)
        {
            return await _restClient.GetAsync<List<Device>>("device" + RestClient.MakeQueryString(filter));
        }

        /// <summary>
        /// Gets information about device.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <returns>A <see cref="Device"/> object.</returns>
        public async Task<Device> GetDeviceAsync(string deviceGuid)
        {
            if (string.IsNullOrEmpty(deviceGuid))
                throw new ArgumentException("DeviceGuid is null or empty!", "deviceGuid");

            return await _restClient.GetAsync<Device>(string.Format("device/{0}", deviceGuid));
        }

        /// <summary>
        /// Gets a list of device equipment states.
        /// These objects provide information about the current state of device equipment.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <returns>A list of <see cref="DeviceEquipmentState"/> objects.</returns>
        public async Task<List<DeviceEquipmentState>> GetEquipmentStateAsync(string deviceGuid)
        {
            if (string.IsNullOrEmpty(deviceGuid))
                throw new ArgumentException("DeviceGuid is null or empty!", "deviceGuid");

            return await _restClient.GetAsync<List<DeviceEquipmentState>>(string.Format("device/{0}/equipment", deviceGuid));
        }

        /// <summary>
        /// Gets a list of notifications generated by the device for the specified filter criteria.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="filter">Notification filter criteria.</param>
        /// <returns>A list of <see cref="Notification"/> objects that match specified filter criteria.</returns>
        public async Task<List<Notification>> GetNotificationsAsync(string deviceGuid, NotificationFilter filter)
        {
            if (string.IsNullOrEmpty(deviceGuid))
                throw new ArgumentException("DeviceGuid is null or empty!", "deviceGuid");

            return await _restClient.GetAsync<List<Notification>>(
                string.Format("device/{0}/notification", deviceGuid) + RestClient.MakeQueryString(filter));
        }

        /// <summary>
        /// Gets a list of commands sent to the device for the specified filter criteria.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="filter">Notification filter criteria.</param>
        /// <returns>A list of <see cref="Command"/> objects that match specified filter criteria.</returns>
        public async Task<List<Command>> GetCommandsAsync(string deviceGuid, CommandFilter filter)
        {
            if (string.IsNullOrEmpty(deviceGuid))
                throw new ArgumentException("DeviceGuid is null or empty!", "deviceGuid");

            return await _restClient.GetAsync<List<Command>>(
                string.Format("device/{0}/command", deviceGuid) + RestClient.MakeQueryString(filter));
        }

        /// <summary>
        /// Gets active subscriptions for DeviceHive commands and notifications.
        /// </summary>
        /// <returns>A list of <see cref="ISubscription"/> objects representing subsription information.</returns>
        public IList<ISubscription> GetSubscriptions()
        {
            if (_channel == null)
                return new List<ISubscription>();

            return _channel.GetSubscriptions();
        }

        /// <summary>
        /// Adds a subscription to device notifications.
        /// Notifications could be sent by devices or by clients on behalf of devices.
        /// </summary>
        /// <param name="deviceGuids">Array of device unique identifiers to subscribe to. Specify null to subscribe to all accessible devices.</param>
        /// <param name="notificationNames">Array of notification names to subsribe to. Specify null to subscribe to all notifications.</param>
        /// <param name="callback">A callback which will be invoken when a notification is retrieved.</param>
        /// <returns>An <see cref="ISubscription"/> object representing the subscription created.</returns>
        public async Task<ISubscription> AddNotificationSubscriptionAsync(string[] deviceGuids, string[] notificationNames, Action<Notification> callback)
        {
            var channel = await OpenChannelAsync();
            return await channel.AddNotificationSubscriptionAsync(deviceGuids, notificationNames, callback);
        }

        /// <summary>
        /// Adds a subscription to device commands.
        /// Commands could only be sent by clients; this subscription would allow to listen to all commands sent to devices.
        /// </summary>
        /// <param name="deviceGuids">Array of device unique identifiers to subscribe to. Specify null to subscribe to all accessible devices.</param>
        /// <param name="commandNames">Array of command names to subsribe to. Specify null to subscribe to all commands.</param>
        /// <param name="callback">A callback which will be invoken when a command is retrieved.</param>
        /// <returns>An <see cref="ISubscription"/> object representing the subscription created.</returns>
        public async Task<ISubscription> AddCommandSubscriptionAsync(string[] deviceGuids, string[] commandNames, Action<Command> callback)
        {
            var channel = await OpenChannelAsync();
            return await channel.AddCommandSubscriptionAsync(deviceGuids, commandNames, callback);
        }

        /// <summary>
        /// Removes an existing subcription.
        /// The method does not throw an exception if subscription has already been removed.
        /// </summary>
        /// <param name="subscription">An <see cref="ISubscription"/> object representing the subscription to remove.</param>
        /// <returns></returns>
        public async Task RemoveSubscriptionAsync(ISubscription subscription)
        {
            var channel = await OpenChannelAsync();
            await channel.RemoveSubscriptionAsync(subscription);
        }

        /// <summary>
        /// Updates device on behalf of device.
        /// </summary>
        /// <param name="device">The <see cref="Device"/> object.</param>
        public async Task UpdateDeviceAsync(Device device)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (string.IsNullOrEmpty(device.Id))
                throw new ArgumentException("Device ID is null or empty", "device.Id");
            //if (string.IsNullOrEmpty(device.Key))
            //    throw new ArgumentException("Device key is null or empty", "device.Key");

            //if (device.Network != null)
            //{
            //    if (string.IsNullOrEmpty(device.Network.Name))
            //        throw new ArgumentException("Device network name is null or empty!", "device.Network.Name");
            //}
            //if (device.DeviceClass != null)
            //{
            //    if (string.IsNullOrEmpty(device.DeviceClass.Name))
            //        throw new ArgumentException("Device class name is null or empty!", "device.DeviceClass.Name");
            //    if (string.IsNullOrEmpty(device.DeviceClass.Version))
            //        throw new ArgumentException("Device class version is null or empty!", "device.DeviceClass.Version");
            //}

            await _restClient.PutAsync(string.Format("device/{0}", device.Id), device);
        }

        /// <summary>
        /// Sends a new notification on behalf of device.
        /// The method sets Id and Timestamp properties of the passed notification in the case of success.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="notification">A <see cref="Notification"/> object representing the notification to be sent.</param>
        /// <returns>Sent Notification object.</returns>
        public async Task<Notification> SendNotificationAsync(string deviceGuid, Notification notification)
        {
            var channel = await OpenChannelAsync();
            return await channel.SendNotificationAsync(deviceGuid, notification);
        }

        /// <summary>
        /// Sends a new command to the device.
        /// The method sets Id, Timestamp and UserId properties of the passed command in the case of success.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="command">A <see cref="Command"/> object representing the command to be sent.</param>
        /// <param name="callback">A callback action to invoke when the command is completed by the device.</param>
        /// <param name="token">Cancellation token to cancel waiting for command result.</param>
        /// <returns>Sent Command object.</returns>
        public async Task<Command> SendCommandAsync(string deviceGuid, Command command, Action<Command> callback = null, CancellationToken? token = null)
        {
            var channel = await OpenChannelAsync();
            return await channel.SendCommandAsync(deviceGuid, command, callback, token);
        }

        /// <summary>
        /// Updates a command on behalf of the device.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="command">A <see cref="Command"/> object to update.</param>
        public async Task UpdateCommandAsync(string deviceGuid, Command command)
        {
            var channel = await OpenChannelAsync();
            await channel.UpdateCommandAsync(deviceGuid, command);
        }

        /// <summary>
        /// Waits until the command is completed and returns a Command object with filled Status and Result properties.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="commandId">Command identifier.</param>
        /// <param name="token">Cancellation token to cancel waiting for command result.</param>
        /// <returns>A <see cref="Command"/> object with filled Status and Result properties.</returns>
        public async Task<Command> WaitCommandResultAsync(string deviceGuid, int commandId, CancellationToken? token = null)
        {
            var channel = await OpenChannelAsync();
            return await channel.WaitCommandResultAsync(deviceGuid, commandId, token);
        }
        #endregion

        #region Channel Functionality

        /// <summary>
        /// Sets an array of available channels to use for maintaining a persistent connection with the DeviceHive server.
        /// The actual channel to be used will be selected as the first object which returns the true <see cref="DeviceHive.Client.Channel.CanConnectAsync()"/> value.
        /// The default list of channels consists of the WebSocketChannel and LongPollingChannel objects.
        /// </summary>
        /// <param name="channels">The array of <see cref="Channel"/> objects to be used.</param>
        public void SetAvailableChannels(Channel[] channels)
        {
            if (channels == null)
                throw new ArgumentNullException("channels");

            if (_channel != null)
                throw new InvalidOperationException("Could not set available channels after a channel has been opened! " +
                    "Please call the SetAvailableChannels before making any actions with DeviceHive.");

            if (_availableChannels != null)
            {
                foreach (var c in _availableChannels)
                    c.StateChanged -= OnChannelStateChanged;
            }

            _availableChannels = channels;

            foreach (var c in _availableChannels)
                c.StateChanged += OnChannelStateChanged;
        }

        /// <summary>
        /// Opens a persistent connection to the DeviceHive server.
        /// The persistent connection could be further used to recieve messages from the server based on subscriptions made.
        /// It is not necessary to call this method before subscribing to messages: corresponding subscription methods will open a channel automatically.
        /// In the case the channel already open, the method returns existing channel object
        /// </summary>
        /// <returns>DeviceHiveChannel object representing persistent connection to the server.</returns>
        public async Task<Channel> OpenChannelAsync()
        {
            if (_channel != null)
                return _channel;

            using (var releaser = await _lock.LockAsync())
            {
                if (_channel == null)
                {
                    Channel channel = null;
                    foreach (var c in _availableChannels)
                    {
                        var canConnect = await c.CanConnectAsync();
                        if (canConnect)
                        {
                            channel = c;
                            break;
                        }
                    }

                    if (channel == null)
                        throw new DeviceHiveException("There are no channels that could be used to connect to the server! " +
                            "Please ensure a compatible channel is registered via a call to the SetAvailableChannels method.");

                    await channel.OpenAsync();
                    _channel = channel;
                }
            }

            return _channel;
        }

        /// <summary>
        /// Closes the persistent connection to the DeviceHive server.
        /// After the channel is closed, all existing subscriptions will be invalidated.
        /// The method does not throw an exception if the channel is not currently open.
        /// </summary>
        /// <returns></returns>
        public async Task CloseChannelAsync()
        {
            if (_channel == null)
                return;

            using (var releaser = await _lock.LockAsync())
            {
                if (_channel != null)
                {
                    await _channel.CloseAsync();
                    _channel = null;
                }
            }
        }

        /// <summary>
        /// Gets a channel object representing the active persistent connection to the DeviceHive server.
        /// This property has a non-null value only if a persistent connection has been previously opened
        /// (e.g. a call to <see cref="OpenChannelAsync"/>, <see cref="AddNotificationSubscriptionAsync"/>, <see cref="SendNotificationAsync"/>, etc. has been previously made).
        /// In the most cases, the callers will not be required to access channel properties and methods directly.
        /// </summary>
        public Channel Channel
        {
            get { return _channel; }
        }

        /// <summary>
        /// Gets the current state of the persistent connection to the DeviceHive server.
        /// </summary>
        public ChannelState ChannelState
        {
            get { return _channel != null ? _channel.State : ChannelState.Disconnected; }
        }

        /// <summary>
        /// Represent an event fired when a channel state changes.
        /// </summary>
        public event EventHandler<ChannelStateEventArgs> ChannelStateChanged;

        #endregion

        #region Protected Methods

        /// <summary>
        /// Called when a channel state changes.
        /// </summary>
        /// <param name="sender">Event sender object.</param>
        /// <param name="eventArgs">A <see cref="ChannelStateEventArgs"/> object.</param>
        protected virtual void OnChannelStateChanged(object sender, ChannelStateEventArgs eventArgs)
        {
            if (ChannelStateChanged != null)
                ChannelStateChanged(sender, eventArgs);
        }

        /// <summary>
        /// Disposes current object
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                var task = CloseChannelAsync();
                // does not need to wait
            }
        }
        #endregion

        #region IDisposable Members

        /// <summary>
        /// Disposes current object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
