#if !EXCLUDE_WEB_SOCKET

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents implementation of the persistent connection to the DeviceHive server using WebSocket API.
    /// </summary>
    public class WebSocketChannel : Channel
    {
        private MessageWebSocket _webSocket;
        private DataWriter _socketWriter;
        private TaskCompletionSource<object> _closeTaskCompletionSource;
        private bool _isClosedByClient;

        private readonly AsyncLock _lock = new AsyncLock(); // synchronizes WebSocket open/close operations
        private readonly Dictionary<string, RequestInfo> _requests = new Dictionary<string, RequestInfo>();

        #region Public Properties

        /// <summary>
        /// The number of miliseconds to wait before the request times out.
        /// The default value is 30,000 milliseconds (30 seconds)
        /// </summary>
        public int Timeout { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="connectionInfo">DeviceHive connection information.</param>
        public WebSocketChannel(DeviceHiveConnectionInfo connectionInfo)
            : this(connectionInfo, null)
        {
        }

        /// <summary>
        /// Constructor which allows to override <see cref="IRestClient" /> which makes HTTP requests to the DeviceHive server.
        /// </summary>
        /// <param name="connectionInfo">DeviceHive connection information.</param>
        /// <param name="restClient">IRestClient implementation.</param>
        public WebSocketChannel(DeviceHiveConnectionInfo connectionInfo, IRestClient restClient)
            : base(connectionInfo, restClient)
        {
            Timeout = 30000;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if current channel object can be used to eshtablish connection to the DeviceHive server.
        /// The method returns true only if the DeviceHive server deployment supports WebSocket protocol.
        /// </summary>
        /// <returns>True if connection can be eshtablished.</returns>
        public override async Task<bool> CanConnectAsync()
        {
            var apiInfo = await GetApiInfoAsync();
            return apiInfo.WebSocketServerUrl != null;
        }

        /// <summary>
        /// Opens a persistent connection to the DeviceHive server.
        /// </summary>
        /// <returns></returns>
        public override async Task OpenAsync()
        {
            using (var releaser = await _lock.LockAsync())
            {
                if (State != ChannelState.Disconnected)
                    throw new InvalidOperationException("The WebSocket connection is already open, please call the CloseAsync method before opening it again!");

                if (!await CanConnectAsync())
                    throw new InvalidOperationException("The WebSocket connection cannot be used since the server does not support it!");

                SetChannelState(ChannelState.Connecting);

                _isClosedByClient = false;

                await OpenWebSocketAsync();
            }
        }

        /// <summary>
        /// Closes the persistent connection to the DeviceHive server.
        /// </summary>
        /// <returns></returns>
        public override async Task CloseAsync()
        {
            using (var releaser = await _lock.LockAsync())
            {
                _isClosedByClient = true;
                if (State == ChannelState.Connected)
                {
                    // close WebSocket, this will trigger HandleConnectionClose handler
                    _webSocket.Close(1000, "Normal Closure");
                    await _closeTaskCompletionSource.Task;
                }
                else if (State == ChannelState.Reconnecting)
                {
                    // set channel state to Disconnected; the Reconnect wait will stop now
                    SetChannelState(ChannelState.Disconnected);
                }
            }
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

            await EnsureConnectedAsync();

            var result = await SendRequestAsync("notification/insert",
                new JProperty("deviceGuid", deviceGuid),
                new JProperty("notification", Serialize(notification)));

            var notificationJson = (JObject)result["notification"];
            var notificationUpdate = Deserialize<Notification>(notificationJson);

            notification.Id = notificationUpdate.Id;
            notification.Timestamp = notificationUpdate.Timestamp;
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
                token = CancellationToken.None;

            await EnsureConnectedAsync();

            var result = await SendRequestAsync("command/insert",
                new JProperty("deviceGuid", deviceGuid),
                new JProperty("command", Serialize(command)));

            var commandJson = (JObject)result["command"];
            var commandUpdate = Deserialize<Command>(commandJson);

            command.Id = commandUpdate.Id;
            command.Timestamp = commandUpdate.Timestamp;
            command.UserId = commandUpdate.UserId;

            if (callback != null && command.Id != null)
                RegisterCommandCallback(command.Id.Value, callback);

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

            await EnsureConnectedAsync();

            var update = new Command { Status = command.Status, Result = command.Result };
            await SendRequestAsync("command/update",
                new JProperty("deviceGuid", deviceGuid),
                new JProperty("commandId", command.Id),
                new JProperty("command", Serialize(update)));
        }
        #endregion

        #region Protected Methods

        /// <summary>
        /// Invoked before new subscription is added.
        /// The methods sends subscribe message to the DeviceHive server.
        /// </summary>
        /// <param name="subscription">A <see cref="ISubscription"/> object representing a subscription.</param>
        /// <returns></returns>
        protected override async Task<Guid> SubscriptionAdding(ISubscription subscription)
        {
            var properties = new List<JProperty>(3);
            properties.Add(new JProperty("timestamp", subscription.Timestamp));
            if (subscription.DeviceGuids != null)
                properties.Add(new JProperty("deviceGuids", new JArray(subscription.DeviceGuids)));
            if (subscription.EventNames != null)
                properties.Add(new JProperty("names", new JArray(subscription.EventNames)));

            var action = subscription.Type == SubscriptionType.Notification ? "notification/subscribe" : "command/subscribe";
            var result = await SendRequestAsync(action, properties.ToArray());
            return result["subscriptionId"] != null ? (Guid)result["subscriptionId"] : Guid.NewGuid();
        }

        /// <summary>
        /// Invoked before an existing subscription is removed.
        /// The methods sends unsubscribe message to the DeviceHive server.
        /// </summary>
        /// <param name="subscription">A <see cref="ISubscription"/> object representing a subscription.</param>
        /// <returns></returns>
        protected override async Task SubscriptionRemoving(ISubscription subscription)
        {
            var properties = new JProperty[] { new JProperty("subscriptionId", subscription.Id) };
            var action = subscription.Type == SubscriptionType.Notification ? "notification/unsubscribe" : "command/unsubscribe";
            await SendRequestAsync(action, properties);
        }
        #endregion

        #region Private Methods

        private async Task OpenWebSocketAsync()
        {
            _closeTaskCompletionSource = new TaskCompletionSource<object>();

            try
            {
                var webSocketUrl = (await GetApiInfoAsync()).WebSocketServerUrl + "/client";

                _webSocket = new MessageWebSocket();
                _webSocket.Control.MessageType = SocketMessageType.Utf8;
                _webSocket.MessageReceived += (s, e) => Task.Run(() => HandleMessage(e));
                _webSocket.Closed += (s, e) => Task.Run(() => HandleConnectionClose());
                await _webSocket.ConnectAsync(new Uri(webSocketUrl));

                _socketWriter = new DataWriter(_webSocket.OutputStream);

                await AuthenticateAsync();

                SetChannelState(ChannelState.Connected);
            }
            catch
            {
                try
                {
                    if (_webSocket != null)
                    {
                        _webSocket.Close(1000, "Abnormal Closure");
                    }
                }
                catch { }
                throw;
            }
        }

        private async Task AuthenticateAsync()
        {
            JProperty[] args;
            if (ConnectionInfo.AccessKey != null)
            {
                args = new[] { new JProperty("accessKey", ConnectionInfo.AccessKey) };
            }
            else
            {
                args = new[] {
                    new JProperty("login", ConnectionInfo.Login),
                    new JProperty("password", ConnectionInfo.Password)
                };
            }

            await SendRequestAsync("authenticate", args);
        }

        private async Task<JObject> SendRequestAsync(string action, params JProperty[] args)
        {
            var requestId = Guid.NewGuid().ToString();
            var requestInfo = new RequestInfo();
            lock (_requests)
            {
                _requests.Add(requestId, requestInfo);
            }

            var commonProperties = new[]
            {
                new JProperty("action", action),
                new JProperty("requestId", requestId)
            };

            var requestJson = new JObject(commonProperties.Concat(args).Cast<object>().ToArray());
            _socketWriter.WriteString(requestJson.ToString());
            await _socketWriter.StoreAsync();

            var resultTask = await Task.WhenAny(requestInfo.Task, Task.Delay(Timeout));
            if (resultTask != requestInfo.Task)
                throw new DeviceHiveException("Timeout while waiting for server response!");

            var result = requestInfo.Task.Result;
            var status = (string)result["status"];
            if (status == "error")
                throw new DeviceHiveException((string)result["error"]);

            return result;
        }

        private void HandleMessage(MessageWebSocketMessageReceivedEventArgs args)
        {
            string message;
            DataReader reader = null;
            try
            {
                try
                {
                    reader = args.GetDataReader();
                }
                catch
                {
                    try
                    {
                        _webSocket.Close(1001, "Abnormal Closure");
                    }
                    catch
                    {
                        HandleConnectionClose();
                    }
                    return;
                }

                reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                message = reader.ReadString(reader.UnconsumedBufferLength);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Dispose();
                }
            }

            var json = JObject.Parse(message);
            var requestId = (string)json["requestId"];

            if (requestId == null)
            {
                // handle server notifications
                var action = (string)json["action"];
                switch (action)
                {
                    case "notification/insert":
                        {
                            var notification = Deserialize<DeviceNotification>(json);
                            InvokeSubscriptionCallback(notification.SubscriptionId, notification.Notification.Timestamp.Value, notification);
                        }
                        return;

                    case "command/insert":
                        {
                            var command = Deserialize<DeviceCommand>(json);
                            InvokeSubscriptionCallback(command.SubscriptionId, command.Command.Timestamp.Value, command);
                        }
                        return;

                    case "command/update":
                        {
                            var command = Deserialize<Command>((JObject)json["command"]);
                            InvokeCommandCallback(command);
                        }
                        return;
                }
            }

            // handle responses to client requests
            RequestInfo requestInfo = null;
            lock (_requests)
            {
                if (_requests.TryGetValue(requestId, out requestInfo))
                    _requests.Remove(requestId);
            }
            if (requestInfo != null)
                requestInfo.SetResult(json);
        }

        private void HandleConnectionClose()
        {
            if (_webSocket != null)
            {
                _webSocket.Dispose();
                _webSocket = null;
            }

            // change channel state
            var tryReconnect = (State == ChannelState.Connected || State == ChannelState.Reconnecting) && !_isClosedByClient;
            if (tryReconnect)
            {
                SetChannelState(ChannelState.Reconnecting);
                Task.Run(async () => await Reconnect());
            }
            else
            {
                SetChannelState(ChannelState.Disconnected);
            }

            // fail pending requests
            var exception = new DeviceHiveException(State == ChannelState.Connected ?
                "WebSocket connection was closed!" : "Could not open a WebSocket connection!");
            lock (_requests)
            {
                foreach (var requestInfo in _requests.Where(r => !r.Value.Task.IsCompleted))
                    requestInfo.Value.SetException(exception);
                _requests.Clear();
            }

            // unblock waiting tasks
            _closeTaskCompletionSource.TrySetResult(true);
        }

        private async Task Reconnect()
        {
            while (State != ChannelState.Connected)
            {
                // wait for some time
                await Task.Delay(1000);
                if (_isClosedByClient)
                    return;

                using (var releaser = await _lock.LockAsync())
                {
                    try
                    {
                        // try opening a WebSocket connection
                        await OpenWebSocketAsync();

                        // restore subscriptions
                        foreach (var subscription in GetSubscriptions().Cast<Subscription>())
                        {
                            subscription.Id = await SubscriptionAdding(subscription);
                        }

                        return; // reconnected
                    }
                    catch
                    {
                        // do nothing, continue reconnecting
                    }
                }
            }
        }
        #endregion

        #region RequestInfo class

        private class RequestInfo
        {
            private readonly TaskCompletionSource<JObject> _taskCompletionSource = new TaskCompletionSource<JObject>();

            public Task<JObject> Task
            {
                get { return _taskCompletionSource.Task; }
            }

            public void SetResult(JObject result)
            {
                _taskCompletionSource.TrySetResult(result);
            }

            public void SetException(Exception exception)
            {
                _taskCompletionSource.TrySetException(exception);
            }
        }
        #endregion
    }
}

#endif