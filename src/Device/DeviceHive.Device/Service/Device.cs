using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DeviceHive.Device
{
    /// <summary>
    /// Represents a DeviceHive device.
    /// </summary>
    public class Device
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets unique device identifier (device-assigned).
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// Gets or sets device key (device-assigned).
        /// Device key is a private value used for device authentication in DeviceHive.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets device name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets initial device status.
        /// </summary>
        /// <remarks>
        /// Device statuses are arbitrary strings. In order to update device status on the DeviceHive service,
        /// devices should call <see cref="IDeviceServiceChannel.SendStatusUpdate"/> method with "status" parameter containing new value.
        /// The DeviceHive server could also automaticall set device status to "Offline",
        /// if the OfflineTimeout property is set on the corresponding device class.
        /// </remarks>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets associated device data.
        /// </summary>
        public JToken Data { get; set; }

        /// <summary>
        /// Gets or sets associated device network object.
        /// </summary>
        public Network Network { get; set; }

        /// <summary>
        /// Gets or sets associated device class object.
        /// </summary>
        public DeviceClass DeviceClass { get; set; }

        /// <summary>
        /// Gets or sets the list of device class equipment.
        /// </summary>
        public List<Equipment> Equipment { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Device()
        {
        }

        /// <summary>
        /// Initializes device unique identifier and key.
        /// </summary>
        /// <param name="id">Device unique identifier (device-assigned).</param>
        /// <param name="key">Device key. Device key is a private value used for device authentication in DeviceHive.</param>
        public Device(Guid? id, string key)
        {
            Id = id;
            Key = key;
        }

        /// <summary>
        /// Initializes all device properties.
        /// </summary>
        /// <param name="id">Device unique identifier (device-assigned).</param>
        /// <param name="key">Device key. Device key is a private value used for device authentication in DeviceHive.</param>
        /// <param name="name">Device name.</param>
        /// <param name="status">Device status.</param>
        /// <param name="data">Device data, an optional json token used to describe additional device information.</param>
        /// <param name="network">Associated device network object (optional).</param>
        /// <param name="deviceClass">Associated device class object.</param>
        public Device(Guid? id, string key, string name, string status, JToken data, Network network, DeviceClass deviceClass)
            : this(id, key)
        {
            Name = name;
            Status = status;
            Data = data;
            Network = network;
            DeviceClass = deviceClass;
        }
        #endregion
    }
}
