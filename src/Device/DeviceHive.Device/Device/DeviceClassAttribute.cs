using System;

namespace DeviceHive.Device
{
    /// <summary>
    /// DeviceClassAttribute set on descendants of the <see cref="DeviceBase"/> class to specify associated device class meta-information.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DeviceClassAttribute : Attribute
    {
        #region Public Properties

        /// <summary>
        /// Gets device class name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets device class version.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Gets or sets timeout in seconds, after which the DeviceHive framework sets device status to Offline.
        /// Set to 0 to disable auto-offline feature.
        /// </summary>
        public int OfflineTimeout { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes device class name and version.
        /// </summary>
        /// <param name="name">Device class name.</param>
        /// <param name="version">Device class version.</param>
        public DeviceClassAttribute(string name, string version)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name is null or empty", "name");
            if (string.IsNullOrEmpty(version))
                throw new ArgumentException("Version is null or empty", "version");

            Name = name;
            Version = version;
        }
        #endregion
    }
}
