using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using WebSocket4Net;

namespace DeviceHive.Client
{
    /// <summary>
    /// Provides access for client to WebSockets DeviceHive API (/client endpoint)
    /// </summary>
    public class WebSocketsClientService : IDisposable
    {
        #region Private fields

        private readonly WebSocket _webSocket;
        private bool _isAuthenticated = false;

        private readonly EventWaitHandle _cancelWaitHandle = new ManualResetEvent(false);
        private readonly EventWaitHandle _authWaitHandle = new ManualResetEvent(false);

        private readonly Dictionary<string, RequestInfo> _requests =
            new Dictionary<string, RequestInfo>();

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="serviceUrl">URL of the DeviceHive web sockets service.</param>
        /// <param name="login">Login used for service authentication.</param>
        /// <param name="password">Password used for service authentication.</param>
        public WebSocketsClientService(string serviceUrl, string login, string password)
        {
            _webSocket = new WebSocket(serviceUrl);
            _webSocket.MessageReceived += (s, e) => HandleMessage(e.Message);
            _webSocket.Opened += (s, e) => Authenticate(login, password);
            _webSocket.Closed += (s, e) => _cancelWaitHandle.Set();
        }
        
        #endregion

        #region Events

        /// <summary>
        /// Fires when new notification is inserted for active device subscription
        /// </summary>
        public event EventHandler<NotificationEventArgs> NotificationInserted;

        /// <summary>
        /// Fires <see cref="NotificationInserted"/> event
        /// </summary>
        /// <param name="deviceGuid">Device GUID</param>
        /// <param name="notification">Notification object</param>
        protected void OnNotificationInserted(Guid deviceGuid, Notification notification)
        {
            var handler = NotificationInserted;
            if (handler != null)
                handler(this, new NotificationEventArgs(deviceGuid, notification));
        }


        /// <summary>
        /// Fires when command submitted by client is updated by device
        /// </summary>
        public event EventHandler<CommandEventArgs> CommandUpdated;

        /// <summary>
        /// Fires <see cref="CommandUpdated"/> event
        /// </summary>
        /// <param name="command">Command object</param>
        protected void OnCommandUpdated(Command command)
        {
            var handler = CommandUpdated;
            if (handler != null)
                handler(this, new CommandEventArgs(command));
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Open WebSocket connection and authenticate user.
        /// </summary>
        public void Open()
        {
            _isAuthenticated = false;

            if (_webSocket.State != WebSocketState.Closed &&
                _webSocket.State != WebSocketState.None)
            {
                _cancelWaitHandle.Reset();
                _webSocket.Close();
                WaitHandle.WaitAny(new WaitHandle[] {_cancelWaitHandle});
            }

            _webSocket.Open();
            WaitHandle.WaitAny(new WaitHandle[] {_authWaitHandle, _cancelWaitHandle});

            if (!_isAuthenticated)
                throw new ClientServiceException("Authentication error");
        }

        /// <summary>
        /// Close WebSocket connection.
        /// </summary>
        public void Close()
        {
            if (_webSocket.State != WebSocketState.Closed && _webSocket.State != WebSocketState.Closing)
                _webSocket.Close();
        }

        /// <summary>
        /// Sends new command to the device.
        /// </summary>
        /// <param name="deviceGuid">Device unique identifier.</param>
        /// <param name="command">A <see cref="Command"/> object to be sent.</param>
        /// <returns>The <see cref="Command"/> object with updated identifier and timestamp.</returns>
        public Command SendCommand(Guid deviceGuid, Command command)
        {
            if (!_isAuthenticated)
                Open();

            var res = SendRequest("command/insert",
                new JProperty("deviceGuid", deviceGuid),
                new JProperty("command", Serialize(command)));
            var commandJson = (JObject) res["command"];
            return Deserialize<Command>(commandJson);
        }

        /// <summary>
        /// Subscribe to device notifications.
        /// </summary>
        /// <param name="deviceGuids">List of device GUIDs. If empty - subscription to all accessible devices
        /// will be created</param>
        public void SubscribeToNotifications(params Guid[] deviceGuids)
        {
            if (!_isAuthenticated)
                Open();

            var properties = new JProperty[0];
            if (deviceGuids.Length >= 0)
                properties = new[] {new JProperty("deviceGuids", new JArray(deviceGuids))};

            SendRequest("notification/subscribe", properties);
        }

        /// <summary>
        /// Unsubscribe from device notifications.
        /// </summary>
        /// <param name="deviceGuids">List of device GUIDs. If empty - subscription to all accessible devices
        /// will be removed</param>
        public void UnsubscribeFromNotifications(params Guid[] deviceGuids)
        {
            if (!_isAuthenticated)
                Open();

            var properties = new JProperty[0];
            if (deviceGuids.Length >= 0)
                properties = new[] { new JProperty("deviceGuids", new JArray(deviceGuids)) };

            SendRequest("notification/unsubscribe", properties);
        }

        #endregion

        #region Private methods

        private void Authenticate(string login, string password)
        {
            SendRequest("authenticate",
                new JProperty("login", login),
                new JProperty("password", password));
            _isAuthenticated = true;
            _authWaitHandle.Set();
        }

        private JObject SendRequest(string action, params JProperty[] args)
        {
            var requestId = Guid.NewGuid().ToString();
            var requestInfo = new RequestInfo();
            _requests.Add(requestId, requestInfo);

            var commonProperties = new[]
            {
                new JProperty("action", action),
                new JProperty("requestId", requestId)
            };

            var requestJson = new JObject(commonProperties.Concat(args).Cast<object>().ToArray());
            _webSocket.Send(requestJson.ToString());

            WaitHandle.WaitAny(new[] {requestInfo.WaitHandle, _cancelWaitHandle});
            
            if (requestInfo.Result == null)
                throw new ClientServiceException("WebSocket connection was unexpectly closed");

            var status = (string) requestInfo.Result["status"];
            if (status == "error")
                throw new ClientServiceException((string) requestInfo.Result["error"]);

            return requestInfo.Result;
        }

        private void HandleMessage(string message)
        {
            var json = JObject.Parse(message);

            // handle server notifications
            var action = (string) json["action"];
            switch (action)
            {
                case "notification/insert":
                    HandleNotificationInsert(json);
                    return;

                case "command/update":
                    HandleCommandUpdate(json);
                    return;
            }

            // handle responses to client requests
            var requestId = (string) json["requestId"];

            RequestInfo requestInfo;
            if (_requests.TryGetValue(requestId, out requestInfo))
                requestInfo.Result = json;
        }

        private void HandleNotificationInsert(JObject json)
        {
            var notificationJson = (JObject) json["notification"];
            var deviceGuid = (Guid) notificationJson["deviceGuid"];
            var notification = Deserialize<Notification>(notificationJson);
            OnNotificationInserted(deviceGuid, notification);
        }

        private void HandleCommandUpdate(JObject json)
        {
            var notificationJson = (JObject) json["command"];
            var command = Deserialize<Command>(notificationJson);
            OnCommandUpdated(command);
        }

        private static JObject Serialize<T>(T obj) where T : class
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            
            var serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.ContractResolver = new JsonContractResolver();
            return JObject.FromObject(obj, serializer);
        }

        private static T Deserialize<T>(JObject json)
        {           
            var serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.ContractResolver = new JsonContractResolver();
            return json.ToObject<T>(serializer);
        }

        #endregion

        #region JsonContractResolver class

        private class JsonContractResolver : CamelCasePropertyNamesContractResolver
        {
            #region DefaultContractResolver Members

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);
                if (property.DeclaringType == typeof(Notification) && property.PropertyName == "name")
                {
                    property.PropertyName = "notification";
                }
                if (property.DeclaringType == typeof(Command) && property.PropertyName == "name")
                {
                    property.PropertyName = "command";
                }
                return property;
            }
            #endregion
        }

        #endregion

        #region RequestInfo class

        private class RequestInfo
        {
            private readonly EventWaitHandle _eventWaitHandle = new ManualResetEvent(false);

            private JObject _result;

            public JObject Result
            {
                get { return _result; }
                set
                {
                    _result = value;
                    _eventWaitHandle.Set();
                }
            }

            public WaitHandle WaitHandle
            {
                get { return _eventWaitHandle; }
            }
        }

        #endregion

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Close();
        }
        
        #endregion
    }
}