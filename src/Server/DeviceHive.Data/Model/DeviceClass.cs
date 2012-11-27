using System;
using System.Collections.Generic;
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
}
