using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using DeviceHive.API.Filters;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.MessageLogic;
using DeviceHive.Core.Messaging;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;

namespace DeviceHive.API.Controllers
{
    /// <resource cref="Device" />
    public class DeviceController : BaseController
    {
        private readonly MessageBus _messageBus;

        public DeviceController(MessageBus messageBus)
        {
            _messageBus = messageBus;
        }

        /// <name>list</name>
        /// <summary>
        /// Gets list of devices.
        /// </summary>
        /// <returns cref="Device">If successful, this method returns array of <see cref="Device"/> resources in the response body.</returns>
        [AuthorizeUser(Roles = "Administrator")]
        public JArray Get()
        {
            return new JArray(DataContext.Device.GetAll().Select(n => Mapper.Map(n)));
        }

        /// <name>get</name>
        /// <summary>
        /// Gets information about device.
        /// </summary>
        /// <param name="id">Device unique identifier.</param>
        /// <returns cref="Device">If successful, this method returns a <see cref="Device"/> resource in the response body.</returns>
        [AuthorizeDeviceOrUser]
        public JObject Get(Guid id)
        {
            EnsureDeviceAccess(id);

            var device = DataContext.Device.Get(id);
            if (device == null || !IsNetworkAccessible(device.NetworkID))
                ThrowHttpResponse(HttpStatusCode.NotFound, "Device not found!");

            return Mapper.Map(device);
        }

        public HttpResponseMessage Post(JObject json)
        {
            return HttpResponse(HttpStatusCode.MethodNotAllowed, "The method is not allowed, please use PUT /device/{id} to register a device");
        }

        /// <name>register</name>
        /// <summary>
        ///     <para>Registers a device.</para>
        ///     <para>If device with specified identifier has already been registered, it gets updated in case when valid key is provided in the authorization header.</para>
        /// </summary>
        /// <param name="id">Device unique identifier.</param>
        /// <param name="json" cref="Device">In the request body, supply a <see cref="Device"/> resource.</param>
        /// <returns cref="Device">If successful, this method returns a <see cref="Device"/> resource in the response body.</returns>
        /// <request>
        ///     <parameter name="network" mode="remove" />
        ///     <parameter name="deviceClass" mode="remove" />
        ///     <parameter name="network" type="integer or object" required="false">
        ///         <para>Network identifier or <see cref="Network"/> object.</para>
        ///         <para>If object is passed, the target network will be searched by name and automatically created if not found.</para>
        ///         <para>In case when existing network is protected with the key, the key value must be included.</para>
        ///     </parameter>
        ///     <parameter name="deviceClass" type="integer or object" required="true">
        ///         <para>Device class identifier or <see cref="DeviceClass"/> object.</para>
        ///         <para>If object is passed, the target device class will be searched by name and version, and automatically created if not found.</para>
        ///         <para>The device class object will be also updated accordingly unless the <see cref="DeviceClass.IsPermanent"/> flag is set.</para>
        ///     </parameter>
        ///     <parameter name="equipment" type="array" required="false" cref="Equipment">
        ///         <para>Array of <see cref="Equipment"/> objects to be associated with the device class. If specified, all existing values will be replaced.</para>
        ///         <para>In case when device class is permanent, this value is ignored.</para>
        ///     </parameter>
        /// </request>
        public JObject Put(Guid id, JObject json)
        {
            // load device from repository
            var device = DataContext.Device.Get(id);
            if (device != null)
            {
                // if device exists, administrator or device authorization is required
                if ((RequestContext.CurrentUser == null || RequestContext.CurrentUser.Role != (int)UserRole.Administrator) &&
                    (RequestContext.CurrentDevice == null || RequestContext.CurrentDevice.GUID != id))
                {
                    ThrowHttpResponse(HttpStatusCode.Unauthorized, "Not authorized");
                }
            }
            else
            {
                // otherwise, create new device
                device = new Device(id);
            }

            // load original device for comparison
            var sourceDevice = device.ID > 0 ? DataContext.Device.Get(device.ID) : null;

            // map and validate the device object
            ResolveNetwork(json);
            ResolveDeviceClass(json, device.ID == 0);
            Mapper.Apply(device, json);
            Validate(device);

            // save device object
            DataContext.Device.Save(device);

            // replace equipments for the corresponding device class
            if (!device.DeviceClass.IsPermanent && json["equipment"] is JArray)
            {
                foreach (var equipment in DataContext.Equipment.GetByDeviceClass(device.DeviceClass.ID))
                {
                    DataContext.Equipment.Delete(equipment.ID);
                }
                foreach (JObject jEquipment in (JArray)json["equipment"])
                {
                    var equipment = GetMapper<Equipment>().Map(jEquipment);
                    equipment.DeviceClass = device.DeviceClass;
                    Validate(equipment);
                    DataContext.Equipment.Save(equipment);
                }
            }

            // save the device diff notification
            var diff = Mapper.Diff(sourceDevice, device);
            var notificationName = sourceDevice == null ? SpecialNotifications.DEVICE_ADD : SpecialNotifications.DEVICE_UPDATE;
            var notification = new DeviceNotification(notificationName, device);
            notification.Parameters = diff.ToString();
            DataContext.DeviceNotification.Save(notification);
            _messageBus.Notify(new DeviceNotificationAddedMessage(device.ID, notification.ID));

            return Mapper.Map(device);
        }

        /// <name>delete</name>
        /// <summary>
        /// Deletes an existing device.
        /// </summary>
        /// <param name="id">Device unique identifier.</param>
        [HttpNoContentResponse]
        [AuthorizeUser(Roles = "Administrator")]
        public void Delete(Guid id)
        {
            var device = DataContext.Device.Get(id);
            if (device != null)
            {
                DataContext.Device.Delete(device.ID);
            }
        }

        private IJsonMapper<Device> Mapper
        {
            get { return GetMapper<Device>(); }
        }

        private void ResolveNetwork(JObject json)
        {
            Network network = null;
            var jNetwork = json.Property("network");
            var verifyNetworkKey = RequestContext.CurrentUser == null;
            if (jNetwork != null && jNetwork.Value is JValue)
            {
                // a value is passed, can be null
                var jNetworkValue = (JValue)jNetwork.Value;
                if (jNetworkValue.Value is long)
                {
                    // search network by ID
                    network = DataContext.Network.Get((int)jNetworkValue);
                    if (verifyNetworkKey && network != null && network.Key != null)
                        ThrowHttpResponse(HttpStatusCode.Forbidden, "Could not register a device because target network is protected with a key!");
                }
            }
            else if (jNetwork != null && jNetwork.Value is JObject)
            {
                // search network by name or auto-create if it does not exist
                var jNetworkObj = (JObject)jNetwork.Value;
                if (jNetworkObj["name"] == null)
                    ThrowHttpResponse(HttpStatusCode.BadRequest, "Specified 'network' object must include 'name' property!");

                network = DataContext.Network.Get((string)jNetworkObj["name"]);
                if (network == null)
                {
                    // auto-create network
                    network = new Network();
                    GetMapper<Network>().Apply(network, jNetworkObj);
                    Validate(network);
                    DataContext.Network.Save(network);
                }

                // check passed network key
                if (verifyNetworkKey && network.Key != null && (string)jNetworkObj["key"] != network.Key)
                    ThrowHttpResponse(HttpStatusCode.Forbidden, "Could not register a device because target network is protected with a key!");

                jNetwork.Value = (long)network.ID;
            }
        }

        private void ResolveDeviceClass(JObject json, bool isRequired)
        {
            var jDeviceClass = json.Property("deviceClass");
            if (isRequired && jDeviceClass == null)
                ThrowHttpResponse(HttpStatusCode.BadRequest, "Required 'deviceClass' property was not specified!");
            
            if (jDeviceClass != null && jDeviceClass.Value is JObject)
            {
                // search device class by name/version or auto-create if it does not exist
                var jDeviceClassObj = (JObject)jDeviceClass.Value;
                if (jDeviceClassObj["name"] == null)
                    ThrowHttpResponse(HttpStatusCode.BadRequest, "Specified 'deviceClass' object must include 'name' property!");
                if (jDeviceClassObj["version"] == null)
                    ThrowHttpResponse(HttpStatusCode.BadRequest, "Specified 'deviceClass' object must include 'version' property!");

                var deviceClass = DataContext.DeviceClass.Get((string)jDeviceClassObj["name"], (string)jDeviceClassObj["version"]);
                if (deviceClass == null)
                {
                    // auto-create device class
                    deviceClass = new DeviceClass();
                }
                if (deviceClass.ID == 0 || !deviceClass.IsPermanent)
                {
                    // auto-update device class if it's not set as permanent
                    GetMapper<DeviceClass>().Apply(deviceClass, jDeviceClassObj);
                    Validate(deviceClass);
                    DataContext.DeviceClass.Save(deviceClass);
                }
                jDeviceClass.Value = (long)deviceClass.ID;
            }
        }
    }
}