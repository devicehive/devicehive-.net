using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using WebSocket4Net;

namespace DeviceHive.Device
{
    /// <summary>
    /// Provides access for devices to WebSockets DeviceHive API (/device endpoint)
    /// </summary>
    public class WebSocketDeviceService
    {
        #region Private fields

        private readonly WebSocket _webSocket;

        private bool _isAuthenticated = false;

        private readonly EventWaitHandle _cancelWaitHandle = new ManualResetEvent(false);
        private readonly EventWaitHandle _authWaitHandle = new ManualResetEvent(false);

        private readonly Dictionary<string, RequestInfo> _requests =
            new Dictionary<string, RequestInfo>();

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

            WaitHandle.WaitAny(new[] { requestInfo.WaitHandle, _cancelWaitHandle });

            if (requestInfo.Result == null)
                throw new ClientServiceException("WebSocket connection was unexpectly closed");

            var status = (string)requestInfo.Result["status"];
            if (status == "error")
                throw new ClientServiceException((string)requestInfo.Result["error"]);

            return requestInfo.Result;
        }

        private void HandleMessage(string message)
        {
            var json = JObject.Parse(message);

            // handle server notifications
            var action = (string)json["action"];
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
            var requestId = (string)json["requestId"];

            RequestInfo requestInfo;
            if (_requests.TryGetValue(requestId, out requestInfo))
                requestInfo.Result = json;
        }

        private void HandleNotificationInsert(JObject json)
        {
            var notificationJson = (JObject)json["notification"];
            var notification = Deserialize<Notification>(notificationJson);
            OnNotificationInserted(notification);
        }

        private void HandleCommandUpdate(JObject json)
        {
            var notificationJson = (JObject)json["command"];
            var command = Deserialize<Command>(notificationJson);
            OnCommandUpdated(command);
        }

        private JObject Serialize<T>(T obj) where T : class
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            var serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.ContractResolver = new JsonContractResolver();
            return JObject.FromObject(obj, serializer);
        }

        private T Deserialize<T>(JObject json)
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