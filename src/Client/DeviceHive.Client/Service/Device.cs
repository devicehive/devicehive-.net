using System;
using System.Collections.Generic;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents a DeviceHive device
    /// </summary>
    public class Device
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets unique device identifier (device-assigned).
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// Gets or sets device name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets device status.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets a dictionary of arbitrary device data.
        /// </summary>
        public Dictionary<string, object> Data { get; set; }

        /// <summary>
        /// Gets or sets associated device network object.
        /// </summary>
        public Network Network { get; set; }

        /// <summary>
        /// Gets or sets associated device class object.
        /// </summary>
        public DeviceClass DeviceClass { get; set; }

        #endregion
    }
}
