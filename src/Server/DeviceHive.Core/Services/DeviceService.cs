using System;
using System.Net;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.MessageLogic;
using DeviceHive.Core.Messaging;
using DeviceHive.Data;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;

namespace DeviceHive.Core.Services
{
    /// <summary>
    /// Incapsulates common <see cref="Device"/> related operations.
    /// </summary>
    public class DeviceService : ServiceBase
    {
        private readonly MessageBus _messageBus;

        /// <summary>
        /// Initialize instance of <see cref="DeviceService"/>
        /// </summary>
        public DeviceService(DataContext dataContext, JsonMapperManager jsonMapperManager,
            MessageBus messageBus) : base(dataContext, jsonMapperManager)
        {
            _messageBus = messageBus;
        }

        /// <summary>
        /// Register new or update existing device
        /// </summary>
        public JObject SaveDevice(Device device, JObject deviceJson,
            bool verifyNetworkKey = true)
        {
            // load original device for comparison
            var sourceDevice = device.ID > 0 ? DataContext.Device.Get(device.ID) : null;

            // map and validate the device object
            var deviceMapper = GetMapper<Device>();

            ResolveNetwork(deviceJson, verifyNetworkKey);
            ResolveDeviceClass(deviceJson, device.ID == 0);
            deviceMapper.Apply(device, deviceJson);
            Validate(device);

            // save device object
            DataContext.Device.Save(device);

            // replace equipments for the corresponding device class
            if (!device.DeviceClass.IsPermanent && deviceJson["equipment"] is JArray)
            {
                foreach (var equipment in DataContext.Equipment.GetByDeviceClass(device.DeviceClass.ID))
                {
                    DataContext.Equipment.Delete(equipment.ID);
                }
                foreach (JObject jEquipment in (JArray)deviceJson["equipment"])
                {
                    var equipment = GetMapper<Equipment>().Map(jEquipment);
                    equipment.DeviceClass = device.DeviceClass;
                    Validate(equipment);
                    DataContext.Equipment.Save(equipment);
                }
            }

            // save the device diff notification
            var diff = deviceMapper.Diff(sourceDevice, device);
            var notificationName = sourceDevice == null ? SpecialNotifications.DEVICE_ADD : SpecialNotifications.DEVICE_UPDATE;
            var notification = new DeviceNotification(notificationName, device);
            notification.Parameters = diff.ToString();
            DataContext.DeviceNotification.Save(notification);
            _messageBus.Notify(new DeviceNotificationAddedMessage(device.ID, notification.ID));

            return deviceMapper.Map(device);
        }

        private void ResolveNetwork(JObject json, bool verifyNetworkKey = true)
        {
            Network network = null;
            var jNetwork = json.Property("network");
            if (jNetwork != null && jNetwork.Value is JValue)
            {
                // a value is passed, can be null
                var jNetworkValue = (JValue)jNetwork.Value;
                if (jNetworkValue.Value is long)
                {
                    // search network by ID
                    network = DataContext.Network.Get((int)jNetworkValue);
                    if (verifyNetworkKey && network != null && network.Key != null)
                        throw new UnauthroizedNetworkException("Could not register a device because target network is protected with a key!");
                }
            }
            else if (jNetwork != null && jNetwork.Value is JObject)
            {
                // search network by name or auto-create if it does not exist
                var jNetworkObj = (JObject)jNetwork.Value;
                if (jNetworkObj["name"] == null)
                    throw new InvalidDataException("Specified 'network' object must include 'name' property!");

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
                    throw new UnauthroizedNetworkException("Could not register a device because target network is protected with a key!");

                jNetwork.Value = (long)network.ID;
            }
        }

        private void ResolveDeviceClass(JObject json, bool isRequired)
        {
            var jDeviceClass = json.Property("deviceClass");
            if (isRequired && jDeviceClass == null)
                throw new InvalidDataException("Required 'deviceClass' property was not specified!");

            if (jDeviceClass != null && jDeviceClass.Value is JObject)
            {
                // search device class by name/version or auto-create if it does not exist
                var jDeviceClassObj = (JObject)jDeviceClass.Value;
                if (jDeviceClassObj["name"] == null)
                    throw new InvalidDataException("Specified 'deviceClass' object must include 'name' property!");
                if (jDeviceClassObj["version"] == null)
                    throw new InvalidDataException("Specified 'deviceClass' object must include 'version' property!");

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