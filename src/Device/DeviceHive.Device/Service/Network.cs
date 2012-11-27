using System;

namespace DeviceHive.Device
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
        /// Gets or sets network key (optional).
        /// If network is protected with a key, devices will need to pass that key in order to register.
        /// </summary>
        public string Key { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Network()
        {
        }

        /// <summary>
        /// Initializes network name and description.
        /// </summary>
        /// <param name="name">Network name</param>
        /// <param name="description">Network description</param>
        public Network(string name, string description)
        {
            Name = name;
            Description = description;
        }

        /// <summary>
        /// Initializes all network properties.
        /// </summary>
        /// <param name="name">Network name</param>
        /// <param name="description">Network description</param>
        /// <param name="key">Network key. If network is protected with a key, devices will need to pass that key in order to register.</param>
        public Network(string name, string description, string key)
            : this(name, description)
        {
            Key = key;
        }
        #endregion
    }
}
