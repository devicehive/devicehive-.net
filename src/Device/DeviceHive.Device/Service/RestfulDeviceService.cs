using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DeviceHive.Device
{
    /// <summary>
    /// Provides default implementation of the <see cref="IDeviceService"/> interface to connect a device with the DeviceHive RESTful service.
    /// Using this class, devices can register, send notifications and receive commands.
    /// </summary>
    public class RestfulDeviceService : IDeviceService
    {
        #region Public Properties

        /// <summary>
        /// Gets URL of the DeviceHive service.
        /// </summary>
        public string ServiceUrl { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="serviceUrl">URL of the DeviceHive service.</param>
        public RestfulDeviceService(string serviceUrl)
        {
            if (serviceUrl == null)
                throw new ArgumentNullException("serviceUrl");

            ServiceUrl = serviceUrl;
        }
        #endregion

        #region IDeviceService Members

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
            return Put(string.Format("/device/{0}", device.Id), device.Id.Value, device.Key, d);
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
        public List<Command> PollCommands(Guid deviceId, string deviceKey, DateTime timestamp, CancellationToken token)
        {
            if (deviceId == Guid.Empty)
                throw new ArgumentException("Device ID is empty!", "deviceId");
            if (string.IsNullOrEmpty(deviceKey))
                throw new ArgumentException("deviceKey is null or empty!", "deviceKey");

            while (true)
            {
                var commands = Get<List<Command>>(string.Format("/device/{0}/command/poll?timestamp={1}",
                    deviceId, timestamp.ToString("yyyy-MM-ddTHH:mm:ss.ffffff")), deviceId, deviceKey, token);
                if (commands != null && commands.Any())
                    return commands;
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
            if (string.IsNullOrEmpty(command.Name))
                throw new ArgumentNullException("Command name is null or empty", "command.Name");

            var c = new Command(command.Name, command.Parameters, command.Status, command.Result);
            Put(string.Format("/device/{0}/command/{1}", deviceId, command.Id), deviceId, deviceKey, c);
        }
        #endregion

        #region Private Methods

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

        private T Put<T>(string url, Guid deviceId, string deviceKey, T obj)
        {
            Logger.Debug("Calling PUT " + url);
            var request = WebRequest.Create(ServiceUrl + url);
            request.Method = "PUT";
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
