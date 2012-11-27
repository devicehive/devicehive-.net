using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DeviceHive.Data.Model
{
    /// <summary>
    /// Represents a device, a unit that runs microcode and communicates to this API.
    /// </summary>
    public class Device
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public Device()
        {
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="guid">Device global identifier</param>
        public Device(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("GUID is empty!", "guid");

            this.GUID = guid;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="guid">Device global identifier</param>
        /// <param name="key">Device key</param>
        /// <param name="name">Device name</param>
        /// <param name="network">Associated network object</param>
        /// <param name="deviceClass">Associated device class object</param>
        public Device(Guid guid, string key, string name, Network network, DeviceClass deviceClass)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("GUID is empty!", "guid");
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key is null or empty!", "key");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name is null or empty!", "name");
            if (deviceClass == null)
                throw new ArgumentNullException("deviceClass");

            this.GUID = guid;
            this.Key = key;
            this.Name = name;
            this.Network = network;
            this.DeviceClass = deviceClass;
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Device identifier.
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// Device unique identifier.
        /// </summary>
        public Guid GUID { get; private set; }

        /// <summary>
        /// Device authentication key.
        /// The key is set during device registration and it has to be provided for all subsequent calls initiated by device.
        /// The key maximum length is 64 characters.
        /// </summary>
        [Required]
        [StringLength(64)]
        public string Key { get; set; }

        /// <summary>
        /// Device display name.
        /// </summary>
        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        /// <summary>
        /// Device operation status.
        /// The status is optional and it can be set to an arbitrary value, if applicable.
        /// <para>To change their status, devices should send 'deviceStatus' notification with the corresponding 'status' parameter.</para>
        /// <para>If device status monitoring feature is enabled, the framework will set status value to 'Offline' after defined period of inactivity.</para>
        /// </summary>
        [StringLength(128)]
        public string Status { get; set; }

        /// <summary>
        /// Associated network identifier.
        /// </summary>
        public int? NetworkID { get; set; }

        /// <summary>
        /// Associated network object.
        /// </summary>
        public Network Network { get; set; }

        /// <summary>
        /// Associated device class identifier.
        /// </summary>
        public int DeviceClassID { get; set; }

        /// <summary>
        /// Associated device class object.
        /// </summary>
        [Required]
        public DeviceClass DeviceClass { get; set; }
        
        #endregion
    }
}
