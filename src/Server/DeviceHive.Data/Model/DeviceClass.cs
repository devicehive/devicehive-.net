using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DeviceHive.Data.Validation;

namespace DeviceHive.Data.Model
{
    /// <summary>
    /// Represents a device class which holds meta-information about devices.
    /// </summary>
    public class DeviceClass
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public DeviceClass()
        {
        }

        /// <summary>
        /// Initializes all required properties
        /// </summary>
        /// <param name="name">Device class name</param>
        /// <param name="version">Device class version</param>
        public DeviceClass(string name, string version)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name is null or empty!", "name");
            if (string.IsNullOrEmpty(version))
                throw new ArgumentException("Version is null or empty!", "version");

            this.Name = name;
            this.Version = version;
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Device class identifier.
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// Device class display name.
        /// </summary>
        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        /// <summary>
        /// Device class version.
        /// </summary>
        [Required]
        [StringLength(32)]
        public string Version { get; set; }

        /// <summary>
        /// Indicates whether device class is permanent.
        /// Permanent device classes could not be modified by devices during registration.
        /// </summary>
        public bool IsPermanent { get; set; }

        /// <summary>
        /// If set, specifies inactivity timeout in seconds before the framework changes device status to 'Offline'.
        /// Device considered inactive when it does not send any notifications.
        /// </summary>
        public int? OfflineTimeout { get; set; }

        /// <summary>
        /// Device class data, a JSON object with an arbitrary structure.
        /// </summary>
        [JsonField]
        public string Data { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents a device class filter.
    /// </summary>
    public class DeviceClassFilter
    {
        #region Public Properties

        /// <summary>
        /// Filter by device class name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Filter by device class name pattern.
        /// </summary>
        public string NamePattern { get; set; }

        /// <summary>
        /// Filter by device class version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Result list sort field. Available values are ID and Name.
        /// </summary>
        [DefaultValue(DeviceClassSortField.None)]
        public DeviceClassSortField SortField { get; set; }

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
    /// Represents device class sort fields.
    /// </summary>
    public enum DeviceClassSortField
    {
        None = 0,
        ID = 1,
        Name = 2
    }
}
