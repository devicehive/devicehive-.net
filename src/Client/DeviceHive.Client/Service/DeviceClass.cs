using System;
using System.Collections.Generic;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents a DeviceHive device class.
    /// Device classes aggregate meta-information about specific type of devices.
    /// </summary>
    public class DeviceClass
	{
        #region Public Properties

        /// <summary>
        /// Gets or sets device class identifier (server-assigned).
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets device class name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets device class version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets offline timeout in seconds, after which the DeviceHive framework sets device status to Offline.
        /// </summary>
        public int? OfflineTimeout { get; set; }

        /// <summary>
        /// Gets or sets the list of device class equipment.
        /// </summary>
        public List<Equipment> Equipment { get; set; }

        #endregion
    }
}
