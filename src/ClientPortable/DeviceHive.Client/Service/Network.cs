using System;
using System.Collections.Generic;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents a DeviceHive network.
    /// Network is used for logical grouping of a set of devices.
    /// </summary>
    public class Network
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets network identifier (server-assigned).
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets network name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets network description (optional).
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a list of associated devices.
        /// </summary>
        public List<Device> Devices { get; set; }

        #endregion
    }
}
