﻿using System;
using System.Collections.Generic;

namespace DeviceHive.Device
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
        /// Gets or sets a dictionary of arbitrary device class data.
        /// </summary>
        public Dictionary<string, object> Data { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DeviceClass()
        {
        }

        /// <summary>
        /// Initializes device class name and version.
        /// </summary>
        /// <param name="name">Device class name.</param>
        /// <param name="version">Device class version.</param>
        public DeviceClass(string name, string version)
        {
            Name = name;
            Version = version;
        }

        /// <summary>
        /// Initializes all device class properties.
        /// </summary>
        /// <param name="name">Device class name.</param>
        /// <param name="version">Device class version.</param>
        /// <param name="offlineTimeout">Device class offline timeout, after which the DeviceHive framework sets device status to Offline.</param>
        /// <param name="data">Device class data, an optional key/value dictionary used to describe additional properties.</param>
        public DeviceClass(string name, string version, int? offlineTimeout, Dictionary<string, object> data)
            : this(name, version)
        {
            OfflineTimeout = offlineTimeout;
            Data = data;
        }
        #endregion
    }
}
