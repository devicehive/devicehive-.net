using System;
using Newtonsoft.Json.Linq;

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
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets device key (used only when updating a device).
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets device name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets device status.
        /// </summary>
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

        #endregion
    }
}
