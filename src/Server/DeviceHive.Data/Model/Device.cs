using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DeviceHive.Data.Validation;

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
        /// Initializes device global identifier
        /// </summary>
        /// <param name="guid">Device global identifier</param>
        public Device(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("GUID is empty!", "guid");

            this.GUID = guid;
        }

        /// <summary>
        /// Initializes all required properties
        /// </summary>
        /// <param name="guid">Device global identifier</param>
        /// <param name="key">Device key</param>
        /// <param name="name">Device name</param>
        /// <param name="network">Associated network object</param>
        /// <param name="deviceClass">Associated device class object</param>
        public Device(Guid guid, string key, string name, Network network, DeviceClass deviceClass)
            : this(guid)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key is null or empty!", "key");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name is null or empty!", "name");
            if (deviceClass == null)
                throw new ArgumentNullException("deviceClass");

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
        /// <para>If device status monitoring feature is enabled, the framework will set status value to 'Offline' after defined period of inactivity.</para>
        /// </summary>
        [StringLength(128)]
        public string Status { get; set; }

        /// <summary>
        /// Device data, a JSON object with an arbitrary structure.
        /// </summary>
        [JsonField]
        public string Data { get; set; }

        /// <summary>
        /// Timestamp of the last online status
        /// </summary>
        public DateTime? LastOnline { get; set; }

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

    /// <summary>
    /// Represents a device filter.
    /// </summary>
    public class DeviceFilter
    {
        #region Public Properties

        /// <summary>
        /// Filter by device name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Filter by device name pattern.
        /// </summary>
        public string NamePattern { get; set; }
        
        /// <summary>
        /// Filter by device status.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Filter by associated network identifier.
        /// </summary>
        public int? NetworkID { get; set; }

        /// <summary>
        /// Filter by associated network name.
        /// </summary>
        public string NetworkName { get; set; }

        /// <summary>
        /// Filter by associated device class identifier.
        /// </summary>
        public int? DeviceClassID { get; set; }

        /// <summary>
        /// Filter by associated device class name.
        /// </summary>
        public string DeviceClassName { get; set; }

        /// <summary>
        /// Filter by associated device class version.
        /// </summary>
        public string DeviceClassVersion { get; set; }

        /// <summary>
        /// Result list sort field. Available values are Name, Status, Network and DeviceClass.
        /// </summary>
        [DefaultValue(DeviceSortField.None)]
        public DeviceSortField SortField { get; set; }

        /// <summary>
        /// Result list sort order. Available values are ASC and DESC.
        /// </summary>
        [DefaultValue(SortOrder.ASC)]
        public SortOrder SortOrder { get; set; }

        /// <summary>
        /// Number of records to skip from the result list.
        /// </summary>
        public int? Skip { get; set; }

        /// <summary>
        /// Number of records to take from the result list.
        /// </summary>
        public int? Take { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents device sort fields.
    /// </summary>
    public enum DeviceSortField
    {
        None = 0,
        ID = 1,
        Name = 2,
        Status = 3,
        Network = 4,
        DeviceClass = 5
    }
}
