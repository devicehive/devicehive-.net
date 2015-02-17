using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace DeviceHive.Client
{
    /// <summary>
    /// Provides default implementation of the <see cref="IClientService"/> interface to connect a client with the RESTful DeviceHive service.
    /// Using this class, clients can get information about networks and devices, receive notification and send commands.
    /// </summary>
    public class RestfulClientService : IClientService, IDisposable
    {
        #region Private Fields

        private WebSocketsClientService _webSocketsClientService;
        
        private Thread _notificationPollThread;
        private Guid[] _notificationSubscriptionDeviceIds;

        private readonly CancellationTokenSource _notificationPollCancellationTokenSource = new CancellationTokenSource();

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets URL of the DeviceHive service.
        /// </summary>
        public string ServiceUrl { get; private set; }
        
        /// <summary>
        /// Gets login used for service authentication.
        /// </summary>
        public string Login { get; private set; }
        
        /// <summary>
        /// Gets password used for service authentication.
        /// </summary>
        public string Password { get; private set; }

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
        /// <param name="login">Login used for service authentication.</param>
        /// <param name="password">Password used for service authentication.</param>
        /// <param name="useWebSockets">Allow using WebSockets API (it it's available).</param>
        public RestfulClientService(string serviceUrl, string login, string password,
            bool useWebSockets = true)
        {
            ServiceUrl = serviceUrl;
            Login = login;
            Password = password;
            UseWebSockets = useWebSockets;
        }
        #endregion

        #region IClientService Members

        /// <summary>
        /// Gets a list of networks.
        /// </summary>
        /// <param name="filter">Network filter.</param>
        /// <returns>A list of <see cref="Network"/> objects that match specified filter criteria.</returns>
        public List<Network> GetNetworks(NetworkFilter filter = null)
        {
            return Get<List<Network>>("/network" + Query(filter));
        }

        /// <summary>
        /// Gets a list of devices of the specific network.
        /// </summary>
        /// <param name="networkId">Network identifier.</param>
        /// <returns>A list of <see cref="Device"/> objects that belongs to the specified network.</returns>
        public List<Device> GetDevices(int networkId)
        {
            return GetDevices(new DeviceFilter { NetworkId = networkId });
        }

        /// <summary>
        /// Gets a list of devices.
        /// </summary>
        /// <param name="filter">Device filter criteria.</param>
        /// <returns>A list of <see cref="Device"/> objects that match specified filter criteria.</returns>
        public List<Device> GetDevices(DeviceFilter filter = null)
        {
            return Get<List<Device>>("/device" + Query(filter));
        }

        /// <summary>
        /// Gets information about device.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <returns><see cref="Device"/> object.</returns>
        public Device GetDevice(Guid deviceId)
        {
            return Get<Device>(string.Format("/device/{0}", deviceId));
        }

        /// <summary>
        /// Gets a list of equipment in a device class.
        /// </summary>
        /// <param name="deviceClassId">Device class identifier.</param>
        /// <returns>A list of <see cref="Equipment"/> objects for the specified device class.</returns>
        public List<Equipment> GetEquipment(int deviceClassId)
		{
            var deviceClass = Get<DeviceClass>(string.Format("/device/class/{0}", deviceClassId));
            return deviceClass == null ? null : deviceClass.Equipment;
		}

        /// <summary>
        /// Gets a list of device equipment states.
        /// These objects provide information about the current state of device equipment.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <returns>A list of <see cref="DeviceEquipmentState"/> objects.</returns>
        public List<DeviceEquipmentState> GetEquipmentState(Guid deviceId)
		{
            return Get<List<DeviceEquipmentState>>(string.Format("/device/{0}/equipment", deviceId));
		}

        /// <summary>
        /// Gets a list of notifications generated by the device.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="start">Notifications start date (inclusive, optional).</param>
        /// <param name="end">Notifications end date (inclusive, optional).</param>
        /// <returns>A list of <see cref="Notification"/> objects.</returns>
        public List<Notification> GetNotifications(Guid deviceId, DateTime? start = null, DateTime? end = null)
        {
            return GetNotifications(deviceId, new NotificationFilter { Start = start, End = end });
        }

        /// <summary>
        /// Gets a list of notifications generated by the device for the specified filter criteria.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="filter">Notification filter criteria.</param>
        /// <returns>A list of <see cref="Notification"/> objects that match specified filter criteria.</returns>
        public List<Notification> GetNotifications(Guid deviceId, NotificationFilter filter)
        {
            return Get<List<Notification>>(string.Format("/device/{0}/notification", deviceId) + Query(filter));
        }

        /// <summary>
        /// Polls device notification from the service.
        /// This methods blocks the current thread until a new command is received.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="timestamp">Last received notification timestamp.</param>
        /// <param name="token">Cancellation token used to cancel the polling operation.</param>
        /// <returns>A list of <see cref="Notification"/> objects.</returns>
        public List<Notification> PollNotifications(Guid deviceId, DateTime? timestamp, CancellationToken token)
        {
            while (true)
            {
                var url = string.Format("/device/{0}/notification/poll", deviceId);
                if (timestamp != null)
                {
                    url += "?timestamp=" + timestamp.Value.ToString("yyyy-MM-ddTHH:mm:ss.ffffff");
                }
                var notifications = Get<List<Notification>>(url, token);
                if (notifications != null && notifications.Any())
                    return notifications;
            }
        }

        /// <summary>
        /// Polls device notification from the service.
        /// This methods blocks the current thread until a new command is received.
        /// </summary>
        /// <param name="deviceIds">List of device unique identifiers.</param>
        /// <param name="timestamp">Last received notification timestamp.</param>
        /// <param name="token">Cancellation token used to cancel the polling operation.</param>
        /// <returns>A list of <see cref="Notification"/> objects.</returns>
        public List<DeviceNotification> PollNotifications(Guid[] deviceIds, DateTime? timestamp, CancellationToken token)
        {
            var url = "/device/notification/poll";

            var parameters = new[]
                {
                    (timestamp == null) ? null :
                        "timestamp=" + timestamp.Value.ToString("yyyy-MM-ddTHH:mm:ss.ffffff"),
                    (deviceIds == null || deviceIds.Length == 0) ? null :
                        "deviceGuids=" + (string.Join(",", deviceIds))
                };

            if (parameters.Any())
                url += "?" + string.Join("&", parameters);

            while (true)
            {
                var notifications = Get<List<DeviceNotification>>(url, token);
                if (notifications != null && notifications.Any())
                    return notifications;
            }
        }

        /// <summary>
        /// Gets information about a notification generated by the device.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="id">Notification identifier.</param>
        /// <returns>The <see cref="Notification"/> object.</returns>
        public Notification GetNotification(Guid deviceId, int id)
        {
            return Get<Notification>(string.Format("/device/{0}/notification/{1}", deviceId, id));
        }

        /// <summary>
        /// Subscribe to device notifications
        /// </summary>
        /// <param name="deviceIds">List of device unique identifiers. If empty - subscription for all
        /// available devices will be created</param>
        /// <remarks>
        /// Subscription can be removed through <see cref="IClientService.UnsubscribeFromNotifications"/> method
        /// </remarks>
        public void SubscribeToNotifications(params Guid[] deviceIds)
        {
            if (InitWebSocketsService())
            {
                _webSocketsClientService.SubscribeToNotifications(deviceIds);
                return;
            }

            if (_notificationSubscriptionDeviceIds == null)
            {
                // there are no any subscriptions yet
                _notificationSubscriptionDeviceIds = deviceIds;
            }
            else if (_notificationSubscriptionDeviceIds.Length > 0)
            {
                // if there is subscription to all devices - we don't need add anything
                _notificationSubscriptionDeviceIds = _notificationSubscriptionDeviceIds
                    .Union(deviceIds)
                    .ToArray();
            }

            RestartNotificationPollThread();
        }

        /// <summary>
        /// Unsubscribe from device notifications
        /// </summary>
        /// <param name="deviceIds">List of device unique identifiers. If empty - subscription for all
        /// available devices will be removed</param>
        public void UnsubscribeFromNotifications(params Guid[] deviceIds)
        {
            if (InitWebSocketsService())
            {
                _webSocketsClientService.UnsubscribeFromNotifications(deviceIds);
                return;
            }

            if (_notificationSubscriptionDeviceIds != null) // there are subscriptions
            {
                if (_notificationSubscriptionDeviceIds.Length == 0) // there is subscription to all devices
                {
                    // it's impossible to remove subscription for concrete device if
                    // there is subscription for all devices
                    if (deviceIds.Length == 0)
                        _notificationSubscriptionDeviceIds = null;
                }
                else
                {
                    _notificationSubscriptionDeviceIds = _notificationSubscriptionDeviceIds
                        .Except(deviceIds)
                        .ToArray();

                    // zero length subscription is subcription to all devices
                    if (_notificationSubscriptionDeviceIds.Length == 0)
                        _notificationSubscriptionDeviceIds = null;
                }
            }

            RestartNotificationPollThread();
        }

        /// <summary>
        /// Gets a list of commands sent to the device.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="start">Commands start date (inclusive, optional).</param>
        /// <param name="end">Commands end date (inclusive, optional).</param>
        /// <returns>A list of <see cref="Command"/> objects.</returns>
        public List<Command> GetCommands(Guid deviceId, DateTime? start = null, DateTime? end = null)
        {
            return GetCommands(deviceId, new CommandFilter { Start = start, End = end });
        }

        /// <summary>
        /// Gets a list of commands sent to the device for the specified filter criteria.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="filter">Notification filter criteria.</param>
        /// <returns>A list of <see cref="Command"/> objects that match specified filter criteria.</returns>
        public List<Command> GetCommands(Guid deviceId, CommandFilter filter)
        {
            return Get<List<Command>>(string.Format("/device/{0}/command", deviceId) + Query(filter));
        }

        /// <summary>
        /// Gets information about a command sent to the device.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="id">Command identifier.</param>
        /// <returns>The <see cref="Command"/> object.</returns>
        public Command GetCommand(Guid deviceId, int id)
        {
            return Get<Command>(string.Format("/device/{0}/command/{1}", deviceId, id));
        }

        /// <summary>
        /// Sends new command to the device.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="command">A <see cref="Command"/> object to be sent.</param>
        /// <returns>The <see cref="Command"/> object with updated identifier and timestamp.</returns>
        public Command SendCommand(Guid deviceId, Command command)
        {
            if (command == null)
                throw new ArgumentNullException("command");

            if (InitWebSocketsService())
                return _webSocketsClientService.SendCommand(deviceId, command);

            return Post<Command>(string.Format("/device/{0}/command", deviceId), command);
        }

        /// <summary>
        /// Waits for a command to be handled by the device.
        /// This methods blocks the current thread until a command is updated on the server by a device.
        /// </summary>
        /// <param name="deviceId">Device unique identifier.</param>
        /// <param name="id">Command identifier.</param>
        /// <param name="token">Cancellation token used to cancel the polling operation.</param>
        /// <returns>The <see cref="Command"/> object with status and result fields.</returns>
        public Command WaitCommand(Guid deviceId, int id, CancellationToken token)
        {
            while (true)
            {
                var command = Get<Command>(string.Format("/device/{0}/command/{1}/poll", deviceId, id), token);
                if (command != null)
                    return command;
            }
        }

        /// <summary>
        /// Gets information about the currently logged-in user.
        /// </summary>
        /// <returns>The <see cref="User"/> object.</returns>
        public User GetCurrentUser()
        {
            return Get<User>("/user/current");
        }

        /// <summary>
        /// Updates the currently logged-in user.
        /// The method only updates the user password.
        /// </summary>
        /// <param name="user">The <see cref="User"/> object with the new password.</param>
        public void UpdateCurrentUser(User user)
        {
            Put<User>("/user/current", user);
        }

        /// <summary>
        /// Fires when new notification inserted for some active notification subscription.
        /// </summary>
        /// <remarks>
        /// Subscription can be created through <see cref="IClientService.SubscribeToNotifications"/> method.
        /// </remarks>
        public event EventHandler<NotificationEventArgs> NotificationInserted;

        /// <summary>
        /// Fires when underlying connection is closed
        /// </summary>
        public event EventHandler ConnectionClosed;

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (_webSocketsClientService != null)
                _webSocketsClientService.Dispose();

            _notificationPollCancellationTokenSource.Cancel();
        }
        #endregion

        #region Protected Methods

        /// <summary>
        /// Fires <see cref="NotificationInserted"/> event
        /// </summary>
        /// <param name="e">Notification event arguments</param>
        protected void OnNotificationInserted(NotificationEventArgs e)
        {
            var handler = NotificationInserted;
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
            if (_webSocketsClientService != null)
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
                var webSocketsClientService = new WebSocketsClientService(serviceUrl + "/client", Login, Password);
                webSocketsClientService.Open();
                webSocketsClientService.NotificationInserted += (s, e) => OnNotificationInserted(e);

                _webSocketsClientService = webSocketsClientService;
                _webSocketsClientService.ConnectionClosed += (s, e) => OnConnectionClosed();

                return true;
            }
            catch (ClientServiceException)
            {
                UseWebSockets = false;
                return false;
            }
        }

        private void RestartNotificationPollThread()
        {
            var apiInfo = Get<ApiInfo>("/info");
            var timestamp = apiInfo.ServerTimestamp;

            if (_notificationPollThread != null && _notificationPollThread.IsAlive)
            {
                _notificationPollCancellationTokenSource.Cancel();
                _notificationPollThread.Join();
            }

            if (_notificationSubscriptionDeviceIds == null)
                return;

            _notificationPollThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        var notifications = PollNotifications(_notificationSubscriptionDeviceIds,
                            timestamp, _notificationPollCancellationTokenSource.Token);

                        foreach (var notification in notifications)
                        {
                            var eventArgs = new NotificationEventArgs(
                                notification.DeviceGuid, notification.Notification);
                            OnNotificationInserted(eventArgs);
                        }

                        timestamp = notifications.Max(n => n.Notification.Timestamp ?? timestamp);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Error on notification polling. Restart polling", e);
                        Thread.Sleep(1000); // retry with small wait
                    }
                }
            });

            _notificationPollThread.Start();
        }

        private T Get<T>(string url)
        {
            Logger.Debug("Calling GET " + url);
            var request = WebRequest.Create(ServiceUrl + url);
            request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(string.Format("{0}:{1}", Login, Password))));
            
            try
            {
                using (var response = request.GetResponse())
                using (var stream = response.GetResponseStream())
                {
                    return Deserialize<T>(stream);
                }
            }
            catch (WebException ex)
            {
                throw new ClientServiceException("Network error while sending request to the server", ex);
            }
        }

        private T Get<T>(string url, CancellationToken token)
        {
            Logger.Debug("Calling GET " + url);
            var request = WebRequest.Create(ServiceUrl + url);
            request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(string.Format("{0}:{1}", Login, Password))));
            var asyncResult = request.BeginGetResponse(null, null);

            // wait for response and throw OperationCancelledException if operation has been cancelled
            if (WaitHandle.WaitAny(new[] { token.WaitHandle, asyncResult.AsyncWaitHandle }) == 0)
            {
                Logger.Debug("Operation has been cancelled: GET " + url);
                token.ThrowIfCancellationRequested();
            }

            try
            {
                using (var response = request.EndGetResponse(asyncResult))
                using (var stream = response.GetResponseStream())
                {
                    return Deserialize<T>(stream);
                }
            }
            catch (WebException ex)
            {
                throw new ClientServiceException("Network error while sending request to the server", ex);
            }
        }

        private T Post<T>(string url, T obj)
        {
            Logger.Debug("Calling POST " + url);
            var request = WebRequest.Create(ServiceUrl + url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(string.Format("{0}:{1}", Login, Password))));
            using (var stream = request.GetRequestStream())
            {
                Serialize(stream, obj);
            }

            try
            {
                using (var response = request.GetResponse())
                using (var stream = response.GetResponseStream())
                {
                    return Deserialize<T>(stream);
                }
            }
            catch (WebException ex)
            {
                throw new ClientServiceException("Network error while sending request to the server", ex);
            }
        }

        private void Put<T>(string url, T obj)
        {
            Logger.Debug("Calling PUT " + url);
            var request = WebRequest.Create(ServiceUrl + url);
            request.Method = "PUT";
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(string.Format("{0}:{1}", Login, Password))));
            using (var stream = request.GetRequestStream())
            {
                Serialize(stream, obj);
            }

            try
            {
                using (var response = request.GetResponse())
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
                throw new ClientServiceException("Network error while sending request to the server", ex);
            }
        }

        private string Query<T>(T query)
        {
            if (query == null)
                return null;

            var serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.DefaultValueHandling = DefaultValueHandling.Ignore;
            serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
            serializer.Converters.Add(new StringEnumConverter());
            serializer.Converters.Add(new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.ffffff" });

            var jObject = JObject.FromObject(query, serializer);
            if (!jObject.Properties().Any())
                return null;

            return "?" + string.Join("&", jObject.Properties().Select(p => p.Name + "=" + Uri.EscapeDataString(p.Value.ToString())));
        }

        private void Serialize<T>(Stream stream, T obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            using (var writer = new StreamWriter(stream))
            {
                var serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                serializer.ContractResolver = new JsonContractResolver();
                serializer.Serialize(writer, obj);
            }
        }
        
        private T Deserialize<T>(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                var serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
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
                return property;
            }
            #endregion
        }
        #endregion
    }
}
