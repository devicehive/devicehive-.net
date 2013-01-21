using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DeviceHive.Device
{
    /// <summary>
    /// Provides default implementation of the <see cref="IDeviceService"/> interface to connect a device with the DeviceHive RESTful service.
    /// Using this class, devices can register, send notifications and receive commands.
    /// </summary>
    public class RestfulDeviceService : IDeviceService, IDisposable
    {
        #region Private fields

        private WebSocketDeviceService _webSocketDeviceService;

        private readonly Dictionary<Guid, CommandSubscriptionTask> _commandSubscriptionTasks =
            new Dictionary<Guid, CommandSubscriptionTask>();

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets URL of the DeviceHive service.
        /// </summary>
        public string ServiceUrl { get; private set; }

        /// <summary>
        /// Gets flag indicating that WebSockets is used when available
        /// </summary>
        public bool UseWebSockets { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="serviceUrl">URL of the DeviceHive service.</param>
        public RestfulDeviceService(string serviceUrl, bool useWebSockets = true)
        {
            if (serviceUrl == null)
                throw new ArgumentNullException("serviceUrl");

            ServiceUrl = serviceUrl;
            UseWebSockets = useWebSockets;
        }
        #endregion

        #region IDeviceService Members

        /// <summary>
        /// Gets device from the DeviceHive network.
        /// </summary>
        /// <param name="device"><see cref="Device"/> object with a valid unique identifier and key.</param>
        /// <returns><see cref="Device"/> object from DeviceHive.</returns>
        public Device GetDevice(Device device)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (device.Id == null)
                throw new ArgumentNullException("device.ID");
            if (string.IsNullOrEmpty(device.Key))
                throw new ArgumentException("Device key is null or empty string", "device.Key");

            if (InitWebSocketsService())
                return _webSocketDeviceService.GetDevice(device.Id.Value, device.Key);

            return Get<Device>(string.Format("/device/{0}", device.Id), device.Id.Value, device.Key);
        }

        /// <summary>
        /// Registers a device in the DeviceHive network.
        /// </summary>
        /// <param name="device"><see cref="Device"/> object.</param>
        /// <returns><see cref="Device"/> object registered.</returns>
        public Device RegisterDevice(Device device)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (device.Id == null)
                throw new ArgumentNullException("device.ID");
            if (string.IsNullOrEmpty(device.Key))
                throw new ArgumentException("Device key is null or empty string", "device.Key");
            if (string.IsNullOrEmpty(device.Name))
                throw new ArgumentException("Device name is null or empty string!", "device.Name");
            if (device.Network != null)
            {
                if (string.IsNullOrEmpty(device.Network.Name))
                    throw new ArgumentException("Device network name is null or empty!", "device.Network.Name");
            }
            if (device.DeviceClass == null)
                throw new ArgumentException("Device class is null!", "device.DeviceClass");
            if (string.IsNullOrEmpty(device.DeviceClass.Name))
                throw new ArgumentException("Device class name is null or empty!", "device.DeviceClass.Name");
            if (string.IsNullOrEmpty(device.DeviceClass.Version))
                throw new ArgumentException("Device class version is null or empty!", "device.DeviceClass.Version");

            var d = new Device(null, device.Key, device.Name, device.Status, device.Data, device.Network, device.DeviceClass) { Equipment = device.Equipment };
            if (InitWebSocketsService())
                return _webSocketDeviceService.RegisterDevice(device.Id, d);

            return Put(string.Format("/device/{0}", device.Id), device.Id.Value, device.Key, d);
        }

        /// <summary>
        /// Updates a device in the DeviceHive network.
        /// </summary>
        /// <param name="device"><see cref="Device"/> object.</param>
        /// <returns><see cref="Device"/> object updated.</returns>
        public Device UpdateDevice(Device device)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (device.Id == null)
                throw new ArgumentNullException("device.ID");
            if (string.IsNullOrEmpty(device.Key))
                throw new ArgumentException("Device key is null or empty string", "device.Key");

            if (device.Network != null)
            {
                if (string.IsNullOrEmpty(device.Network.Name))
                    throw new ArgumentException("Device network name is null or empty!", "device.Network.Name");
            }
            if (device.DeviceClass != null)
            {
                if (string.IsNullOrEmpty(device.DeviceClass.Name))
                    throw new ArgumentException("Device class name is null or empty!", "device.DeviceClass.Name");
                if (string.IsNullOrEmpty(device.DeviceClass.Version))
                    throw new ArgumentException("Device class version is null or empty!", "device.DeviceClass.Version");
            }

            var d = new Device(null, device.Key, device.Name, device.Status, device.Data, device.Network, device.DeviceClass) { Equipment = device.Equipment };
            if (InitWebSocketsService())
                return _webSocketDeviceService.UpdateDevice(d, device.Id.Value, device.Key);

            return Put(string.Format("/device/{0}", device.Id), device.Id.Value, device.Key, d, NullValueHandling.Ignore);
        }

        /// <summary>
        /// Sends new device notification to the service.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="deviceKey">Device key.</param>
        /// <param name="notification">A <see cref="Notification"/> object</param>
        /// <returns>The <see cref="Notification"/> object with updated identifier and timestamp.</returns>
        public Notification SendNotification(Guid deviceId, string deviceKey, Notification notification)
        {
            if (deviceId == Guid.Empty)
                throw new ArgumentException("Device ID is empty!", "deviceId");
            if (string.IsNullOrEmpty(deviceKey))
                throw new ArgumentException("deviceKey is null or empty!", "deviceKey");
            if (notification == null)
                throw new ArgumentNullException("notification");
            if (string.IsNullOrEmpty(notification.Name))
                throw new ArgumentNullException("Notification name is null or empty", "notification.Name");

            if (InitWebSocketsService())
                return _webSocketDeviceService.SendNotification(notification, deviceId, deviceKey);

            return Post(string.Format("/device/{0}/notification", deviceId), deviceId, deviceKey, notification);
        }

        /// <summary>
        /// Polls device commands from the service.
        /// This methods blocks the current thread until new command is received.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="deviceKey">Device key.</param>
        /// <param name="timestamp">Last received command timestamp.</param>
        /// <param name="token">Cancellation token used to cancel polling operation.</param>
        /// <returns>A list of <see cref="Command"/> objects.</returns>
        public List<Command> PollCommands(Guid deviceId, string deviceKey, DateTime? timestamp, CancellationToken token)
        {
            if (deviceId == Guid.Empty)
                throw new ArgumentException("Device ID is empty!", "deviceId");
            if (string.IsNullOrEmpty(deviceKey))
                throw new ArgumentException("deviceKey is null or empty!", "deviceKey");

            while (true)
            {
                var url = string.Format("/device/{0}/command/poll", deviceId);
                if (timestamp != null)
                {
                    url += "?timestamp=" + timestamp.Value.ToString("yyyy-MM-ddTHH:mm:ss.ffffff");
                }
                var commands = Get<List<Command>>(url, deviceId, deviceKey, token);
                if (commands != null && commands.Any())
                    return commands;
            }
        }

        /// <summary>
        /// Fires when new command inserted for some active command subscription.
        /// </summary>
        /// <remarks>
        /// Subscription can be created through <see cref="IDeviceService.SubscribeToCommands"/> method.
        /// </remarks>
        public event EventHandler<CommandEventArgs> CommandInserted;

        /// <summary>
        /// Fires when underlying connection is closed
        /// </summary>
        public event EventHandler ConnectionClosed;

        /// <summary>
        /// Subscribe to device commands
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="deviceKey">Device key.</param>
        /// <remarks>
        /// Subscription can be removed through <see cref="UnsubscribeFromCommands"/> method
        /// </remarks>
        public void SubscribeToCommands(Guid deviceId, string deviceKey)
        {
            if (InitWebSocketsService())
            {
                _webSocketDeviceService.SubscribeToCommands(deviceId, deviceKey);
                return;
            }

            if (_commandSubscriptionTasks.ContainsKey(deviceId))
                return;

            lock (_commandSubscriptionTasks)
            {
                if (_commandSubscriptionTasks.ContainsKey(deviceId))
                    return;

                var task = new CommandSubscriptionTask(this, deviceId, deviceKey);
                _commandSubscriptionTasks.Add(deviceId, task);
            }
        }

        /// <summary>
        /// Unsubscribe from device notifications
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="deviceKey">Device key.</param>
        public void UnsubscribeFromCommands(Guid deviceId, string deviceKey)
        {
            if (InitWebSocketsService())
            {
                _webSocketDeviceService.UnsubscribeFromCommands(deviceId, deviceKey);
                return;
            }

            if (!_commandSubscriptionTasks.ContainsKey(deviceId))
                return;

            lock (_commandSubscriptionTasks)
            {
                CommandSubscriptionTask task;
                if (!_commandSubscriptionTasks.TryGetValue(deviceId, out task))
                    return;

                task.Cancel();
            }
        }

        /// <summary>
        /// Updates a device command status and result.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="deviceKey">Device key.</param>
        /// <param name="command">A <see cref="Command"/> object to be updated.</param>
        public void UpdateCommand(Guid deviceId, string deviceKey, Command command)
        {
            if (deviceId == Guid.Empty)
                throw new ArgumentException("Device ID is empty!", "deviceId");
            if (string.IsNullOrEmpty(deviceKey))
                throw new ArgumentException("deviceKey is null or empty!", "deviceKey");
            if (command == null)
                throw new ArgumentNullException("command");
            if (command.Id == null)
                throw new ArgumentNullException("command.ID");

            var c = new Command(null, null, command.Status, command.Result);

            if (InitWebSocketsService())
            {
                _webSocketDeviceService.UpdateCommand(c, deviceId, deviceKey);
            }
            else
            {
                Put(string.Format("/device/{0}/command/{1}", deviceId, command.Id),
                    deviceId, deviceKey, c, NullValueHandling.Ignore);
            }
        }
        #endregion

        #region Protected methods

        /// <summary>
        /// Fires <see cref="CommandInserted"/> event
        /// </summary>
        /// <param name="e">Command event arguments</param>
        protected void OnCommandInserted(CommandEventArgs e)
        {
            var handler = CommandInserted;
            if (handler != null)
                handler(this, e);
        }

        /// <summary>
        /// Fires <see cref="ConnectionClosed"/> event
        /// </summary>
        protected void OnConnectionClosed()
        {
            var handler = ConnectionClosed;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        #endregion

        #region Private Methods

        private bool InitWebSocketsService()
        {
            if (_webSocketDeviceService != null)
                return true;

            if (!UseWebSockets)
                return false;

            var apiInfo = Get<ApiInfo>("/info");
            var serviceUrl = apiInfo.WebSocketServerUrl;

            if (serviceUrl == null)
            {
                UseWebSockets = false;
                return false;
            }

            try
            {
                var webSocketDeviceService = new WebSocketDeviceService(serviceUrl + "/device");
                webSocketDeviceService.Open();
                webSocketDeviceService.CommandInserted += (s, e) => OnCommandInserted(e);
                
                _webSocketDeviceService = webSocketDeviceService;
                _webSocketDeviceService.ConnectionClosed += (s, e) => OnConnectionClosed();

                return true;
            }
            catch (DeviceServiceException)
            {
                UseWebSockets = false;
                return false;
            }
        }

        private T Get<T>(string url)
        {
            Logger.Debug("Calling GET " + url);
            var request = WebRequest.Create(ServiceUrl + url);
            try
            {
                var response = request.GetResponse();
                using (var stream = response.GetResponseStream())
                {
                    return Deserialize<T>(stream);
                }
            }
            catch (WebException ex)
            {
                throw new DeviceServiceException("Network error while sending request to the server", ex);
            }
        }

        private T Get<T>(string url, Guid deviceId, string deviceKey)
        {
            Logger.Debug("Calling GET " + url);
            var request = WebRequest.Create(ServiceUrl + url);
            request.Headers["Auth-DeviceID"] = deviceId.ToString();
            request.Headers["Auth-DeviceKey"] = deviceKey;
            try
            {
                var response = request.GetResponse();
                using (var stream = response.GetResponseStream())
                {
                    return Deserialize<T>(stream);
                }
            }
            catch (WebException ex)
            {
                throw new DeviceServiceException("Network error while sending request to the server", ex);
            }
        }

        private T Get<T>(string url, Guid deviceId, string deviceKey, CancellationToken token)
        {
            Logger.Debug("Calling GET " + url);
            var request = WebRequest.Create(ServiceUrl + url);
            request.Headers["Auth-DeviceID"] = deviceId.ToString();
            request.Headers["Auth-DeviceKey"] = deviceKey;
            var asyncResult = request.BeginGetResponse(null, null);

            // wait for response and throw OperationCancelledException if operation has been cancelled
            if (WaitHandle.WaitAny(new[] { token.WaitHandle, asyncResult.AsyncWaitHandle }) == 0)
            {
                Logger.Debug("Operation has been cancelled: GET " + url);
                token.ThrowIfCancellationRequested();
            }

            try
            {
                var response = request.EndGetResponse(asyncResult);
                using (var stream = response.GetResponseStream())
                {
                    return Deserialize<T>(stream);
                }
            }
            catch (WebException ex)
            {
                throw new DeviceServiceException("Network error while sending request to the server", ex);
            }
        }

        private T Post<T>(string url, Guid deviceId, string deviceKey, T obj)
        {
            Logger.Debug("Calling POST " + url);
            var request = WebRequest.Create(ServiceUrl + url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers["Auth-DeviceID"] = deviceId.ToString();
            request.Headers["Auth-DeviceKey"] = deviceKey;
            using (var stream = request.GetRequestStream())
            {
                Serialize(stream, obj);
            }

            try
            {
                var response = request.GetResponse();
                using (var stream = response.GetResponseStream())
                {
                    return Deserialize<T>(stream);
                }
            }
            catch (WebException ex)
            {
                throw new DeviceServiceException("Network error while sending request to the server", ex);
            }
        }

        private T Put<T>(string url, Guid deviceId, string deviceKey, T obj, NullValueHandling nullValueHandling = NullValueHandling.Include)
        {
            Logger.Debug("Calling PUT " + url);
            var request = WebRequest.Create(ServiceUrl + url);
            request.Method = "PUT";
            request.ContentType = "application/json";
            request.Headers["Auth-DeviceID"] = deviceId.ToString();
            request.Headers["Auth-DeviceKey"] = deviceKey;
            using (var stream = request.GetRequestStream())
            {
                Serialize(stream, obj, nullValueHandling);
            }

            try
            {
                var response = request.GetResponse();
                using (var stream = response.GetResponseStream())
                {
                    return Deserialize<T>(stream);
                }
            }
            catch (WebException ex)
            {
                throw new DeviceServiceException("Network error while sending request to the server", ex);
            }
        }

        private void Delete(string url, Guid deviceId, string deviceKey)
        {
            Logger.Debug("Calling DELETE " + url);
            var request = WebRequest.Create(ServiceUrl + url);
            request.Method = "DELETE";
            request.Headers["Auth-DeviceID"] = deviceId.ToString();
            request.Headers["Auth-DeviceKey"] = deviceKey;
            try
            {
                var response = request.GetResponse();
                using (var stream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        reader.ReadToEnd();
                    }
                }
            }
            catch (WebException ex)
            {
                throw new DeviceServiceException("Network error while sending request to the server", ex);
            }
        }

        private void Serialize<T>(Stream stream, T obj, NullValueHandling nullValueHandling = NullValueHandling.Include)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            using (var writer = new StreamWriter(stream))
            {
                var serializer = new JsonSerializer();
                serializer.NullValueHandling = nullValueHandling;
                serializer.ContractResolver = new JsonContractResolver();
                serializer.Serialize(writer, obj);
            }
        }

        private T Deserialize<T>(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                var serializer = new JsonSerializer();
                serializer.ContractResolver = new JsonContractResolver();
                return (T)serializer.Deserialize(reader, typeof(T));
            }
        }
        #endregion

        #region Private Properties

        private ILog Logger
        {
            get { return LogManager.GetLogger(GetType()); }
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
                if (property.DeclaringType == typeof(Device) && property.PropertyName == "network")
                {
                    property.NullValueHandling = NullValueHandling.Ignore;
                }
                if (property.PropertyName == "id")
                {
                    property.NullValueHandling = NullValueHandling.Ignore;
                }
                return property;
            }
            #endregion
        }
        #endregion

        #region CommandSubscriptionTask class

        private class CommandSubscriptionTask
        {
            private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

            public CommandSubscriptionTask(RestfulDeviceService restfulDeviceService,
                Guid deviceId, string deviceKey)
            {
                var apiInfo = restfulDeviceService.Get<ApiInfo>("/info");
                var timestamp = apiInfo.ServerTimestamp;

                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        while (true)
                        {
                            var сommands = restfulDeviceService.PollCommands(deviceId, deviceKey,
                                timestamp, _cancellationTokenSource.Token);

                            foreach (var command in сommands)
                            {
                                var eventArgs = new CommandEventArgs(deviceId, command);
                                restfulDeviceService.OnCommandInserted(eventArgs);
                            }

                            timestamp = сommands.Max(c => c.Timestamp ?? timestamp);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                });
            }

            public void Cancel()
            {
                _cancellationTokenSource.Cancel();
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
            if (_webSocketDeviceService != null)
                _webSocketDeviceService.Dispose();

            foreach (var commandSubscriptionTask in _commandSubscriptionTasks.Values)
                commandSubscriptionTask.Cancel();
        }

        #endregion
    }
}
