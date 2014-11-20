using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents implementation of the persistent connection to the DeviceHive server using WebSocket API.
    /// </summary>
    public class WebSocketChannel : Channel
    {
        private WebSocket _webSocket;
        private TaskCompletionSource<object> _authTaskCompletionSource;
        private TaskCompletionSource<object> _closeTaskCompletionSource;

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
            : base(connectionInfo)
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
                    throw new InvalidOperationException("The WebSocket connection is already open, please call the Close method before opening it again!");

                if (!await CanConnectAsync())
                    throw new InvalidOperationException("The WebSocket connection cannot be used since the server does not support it!");

                SetChannelState(ChannelState.Connecting);

                var webSocketUrl = (await GetApiInfoAsync()).WebSocketServerUrl + "/client";
                _webSocket = new WebSocket(webSocketUrl);
                _webSocket.MessageReceived += (s, e) => Task.Run(() => HandleMessage(e.Message));
                _webSocket.Opened += (s, e) => Task.Run(() => Authenticate());
                _webSocket.Closed += (s, e) =>
                    {
                        var exception = new DeviceHiveException("WebSocket connection was closed!");

                        if (_authTaskCompletionSource != null && !_authTaskCompletionSource.Task.IsCompleted)
                            _authTaskCompletionSource.SetException(exception);

                        lock (_requests)
                        {
                            foreach (var requestInfo in _requests.Where(r => !r.Value.Task.IsCompleted))
                                requestInfo.Value.SetException(exception);
                            _requests.Clear();
                        }

                        _webSocket = null;
                        SetChannelState(ChannelState.Disconnected);
                        _closeTaskCompletionSource.SetResult(true);
                    };

                _closeTaskCompletionSource = new TaskCompletionSource<object>();
                _authTaskCompletionSource = new TaskCompletionSource<object>();

                try
                {
                    _webSocket.Open();

                    var resultTask = await Task.WhenAny(_authTaskCompletionSource.Task, Task.Delay(Timeout));
                    if (resultTask != _authTaskCompletionSource.Task)
                        throw new DeviceHiveException("Timeout while waiting for authentication response!");

                    _authTaskCompletionSource.Task.Wait(); // throw exception if authentication failed
                    SetChannelState(ChannelState.Connected);
                }
                catch (Exception)
                {
                    CloseAsync().Wait();
                    throw;
                }
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
                if (State != ChannelState.Disconnected)
                {
                    _webSocket.Close();
                    await _closeTaskCompletionSource.Task;
                }
            }
        }

        /// <summary>
        /// Sends a notification on behalf of device.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="notification">A <see cref="Notification"/> object to be sent.</param>
        public override async Task SendNotificationAsync(string deviceGuid, Notification notification)
        {
            if (string.IsNullOrEmpty(deviceGuid))
                throw new ArgumentException("DeviceGuid is null or empty!", "deviceGuid");
            if (notification == null)
                throw new ArgumentNullException("notification");

            CheckConnection();

            var result = await SendRequest("notification/insert",
                new JProperty("deviceGuid", deviceGuid),
                new JProperty("notification", Serialize(notification)));

            var notificationJson = (JObject)result["notification"];
            var notificationUpdate = Deserialize<Notification>(notificationJson);

            notification.Id = notificationUpdate.Id;
            notification.Timestamp = notificationUpdate.Timestamp;
        }

        /// <summary>
        /// Sends a command to the device.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="command">A <see cref="Command"/> object to be sent.</param>
        /// <param name="callback">A callback action to invoke when the command is completed by the device.</param>
        /// <param name="token">Cancellation token to cancel polling command result.</param>
        public override async Task SendCommandAsync(string deviceGuid, Command command, Action<Command> callback = null, CancellationToken? token = null)
        {
            if (string.IsNullOrEmpty(deviceGuid))
                throw new ArgumentException("DeviceGuid is null or empty!", "deviceGuid");
            if (command == null)
                throw new ArgumentNullException("command");
            if (!token.HasValue)
                token = CancellationToken.None;

            CheckConnection();

            var result = await SendRequest("command/insert",
                new JProperty("deviceGuid", deviceGuid),
                new JProperty("command", Serialize(command)));

            var commandJson = (JObject)result["command"];
            var commandUpdate = Deserialize<Command>(commandJson);

            command.Id = commandUpdate.Id;
            command.Timestamp = commandUpdate.Timestamp;
            command.UserId = commandUpdate.UserId;

            if (callback != null && command.Id != null)
                RegisterCommandCallback(command.Id.Value, callback);
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

            CheckConnection();

            var update = new Command { Status = command.Status, Result = command.Result };
            await SendRequest("command/update",
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
            var properties = new List<JProperty>(2);
            if (subscription.DeviceGuids != null)
                properties.Add(new JProperty("deviceGuids", new JArray(subscription.DeviceGuids)));
            if (subscription.EventNames != null)
                properties.Add(new JProperty("names", new JArray(subscription.EventNames)));

            var action = subscription.Type == SubscriptionType.Notification ? "notification/subscribe" : "command/subscribe";
            var result = await SendRequest(action, properties.ToArray());
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
            await SendRequest(action, properties);
        }
        #endregion

        #region Private Methods

        private async Task Authenticate()
        {
            try
            {
                await SendRequest("authenticate",
                    new JProperty("login", ConnectionInfo.Login),
                    new JProperty("password", ConnectionInfo.Password));

                _authTaskCompletionSource.SetResult(true);
            }
            catch (Exception ex)
            {
                _authTaskCompletionSource.SetException(ex);
            }
        }

        private async Task<JObject> SendRequest(string action, params JProperty[] args)
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
            _webSocket.Send(requestJson.ToString());

            var resultTask = await Task.WhenAny(requestInfo.Task, Task.Delay(Timeout));
            if (resultTask != requestInfo.Task)
                throw new DeviceHiveException("Timeout while waiting for server response!");

            var result = requestInfo.Task.Result;
            var status = (string)result["status"];
            if (status == "error")
                throw new DeviceHiveException((string)result["error"]);

            return result;
        }

        private void HandleMessage(string message)
        {
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
                            InvokeSubscriptionCallback(notification);
                        }
                        return;

                    case "command/insert":
                        {
                            var command = Deserialize<DeviceCommand>(json);
                            InvokeSubscriptionCallback(command);
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
            lock (_requests)
            {
                RequestInfo requestInfo;
                if (_requests.TryGetValue(requestId, out requestInfo))
                {
                    requestInfo.SetResult(json);
                    _requests.Remove(requestId);
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
                _taskCompletionSource.SetResult(result);
            }

            public void SetException(Exception exception)
            {
                _taskCompletionSource.SetException(exception);
            }
        }
        #endregion
    }
}
