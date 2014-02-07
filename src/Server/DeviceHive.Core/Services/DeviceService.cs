using System;
using System.Collections.Generic;
using System.Configuration;
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
        private readonly DeviceHiveConfiguration _configuration;

        /// <summary>
        /// Initialize instance of <see cref="DeviceService"/>
        /// </summary>
        public DeviceService(DataContext dataContext, JsonMapperManager jsonMapperManager,
            MessageBus messageBus, DeviceHiveConfiguration configuration) : base(dataContext, jsonMapperManager)
        {
            _messageBus = messageBus;
            _configuration = configuration;
        }

        /// <summary>
        /// Register new or update existing device.
        /// </summary>
        /// <param name="device">Source device to update.</param>
        /// <param name="deviceJson">A JSON object with device properties.</param>
        /// <param name="verifyNetworkKey">Whether to verify if a correct network key is passed.</param>
        /// <param name="networkAccessCheck">An optional delegate to verify network access control.</param>
        public JObject SaveDevice(Device device, JObject deviceJson,
            bool verifyNetworkKey = true, Func<Network, bool> networkAccessCheck = null)
        {
            // load original device for comparison
            var sourceDevice = device.ID > 0 ? DataContext.Device.Get(device.ID) : null;

            // map and validate the device object
            var deviceMapper = GetMapper<Device>();
            deviceMapper.Apply(device, deviceJson);
            Validate(device);

            // resolve and verify associated objects
            ResolveNetwork(device, verifyNetworkKey, networkAccessCheck);
            ResolveDeviceClass(device);

            // save device object
            DataContext.Device.Save(device);

            // backward compatibility with 1.2 - equipment was passed on device level
            if (!device.DeviceClass.IsPermanent && deviceJson["equipment"] is JArray)
            {
                device.DeviceClass.Equipment = new List<Equipment>();
                foreach (JObject jEquipment in (JArray)deviceJson["equipment"])
                {
                    var equipment = GetMapper<Equipment>().Map(jEquipment);
                    Validate(equipment);
                    device.DeviceClass.Equipment.Add(equipment);
                }
                DataContext.DeviceClass.Save(device.DeviceClass);
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

        private void ResolveNetwork(Device device, bool verifyNetworkKey, Func<Network, bool> networkAccessCheck)
        {
            if (device.Network == null)
                return; // device could have no network assigned

            var network = (Network)null;
            if (device.Network.Name != null)
            {
                // network name is passed
                network = DataContext.Network.Get(device.Network.Name);
                if (network == null)
                {
                    // auto-create network - only for test environments
                    if (!_configuration.Network.AllowAutoCreate)
                        throw new UnauthroizedNetworkException("Automatic network creation is not allowed, please specify an existing network!");

                    network = device.Network;
                    Validate(network);
                    DataContext.Network.Save(network);
                }
                else
                {
                    if (verifyNetworkKey && network.Key != null && network.Key != device.Network.Key)
                        throw new UnauthroizedNetworkException("Could not register a device because target network is protected with a key!");
                    if (networkAccessCheck != null && !networkAccessCheck(network))
                        throw new UnauthroizedNetworkException("Could not register a device because target network is not accessible!");
                }
            }
            else
            {
                throw new InvalidDataException("Specified 'network' object must include 'name' property!");
            }

            device.Network = network;
        }

        private void ResolveDeviceClass(Device device)
        {
            if (device.DeviceClass == null)
                throw new InvalidDataException("Required 'deviceClass' property can not be null!");

            var deviceClass = (DeviceClass)null;
            if (device.DeviceClass.Name != null && device.DeviceClass.Version != null)
            {
                // device class name and version are passed
                deviceClass = DataContext.DeviceClass.Get(device.DeviceClass.Name, device.DeviceClass.Version);
                if (deviceClass == null)
                {
                    // auto-create device class
                    deviceClass = device.DeviceClass;

                    Validate(deviceClass);
                    if (deviceClass.Equipment != null)
                        deviceClass.Equipment.ForEach(e => Validate(e));
                    DataContext.DeviceClass.Save(deviceClass);
                }
                else if (!deviceClass.IsPermanent)
                {
                    // auto-update device class if it's not set as permanent
                    deviceClass.Data = device.DeviceClass.Data;
                    deviceClass.IsPermanent = device.DeviceClass.IsPermanent;
                    deviceClass.OfflineTimeout = device.DeviceClass.OfflineTimeout;
                    if (device.DeviceClass.Equipment != null)
                    {
                        deviceClass.Equipment.Clear();
                        deviceClass.Equipment.AddRange(device.DeviceClass.Equipment);
                    }
                    
                    Validate(deviceClass);
                    deviceClass.Equipment.ForEach(e => Validate(e));
                    DataContext.DeviceClass.Save(deviceClass);
                }
            }
            else
            {
                throw new InvalidDataException("Specified 'deviceClass' object must include 'name' and 'version' properties!");
            }

            device.DeviceClass = deviceClass;
        }
    }
}