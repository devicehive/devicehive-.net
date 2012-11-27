using System;

namespace DeviceHive.Device
{
    /// <summary>
    /// DeviceAttribute set on descendants of the <see cref="DeviceBase"/> class to specify primary device characteristics.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DeviceAttribute : Attribute
    {
        #region Public Properties

        /// <summary>
        /// Gets device unique identifier.
        /// </summary>
        public Guid ID { get; private set; }

        /// <summary>
        /// Gets device key used for device authentication in DeviceHive.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Gets device name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the flag indicating whether device should listen for commands sent to the device.
        /// If true, the device will listen to commands. Otherwise, the device will ignore all incoming commands.
        /// </summary>
        public bool ListenCommands { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes device unique identifier, device key and device name.
        /// </summary>
        /// <param name="id">Device unique identifier.</param>
        /// <param name="key">Device key used for device authentication in DeviceHive.</param>
        /// <param name="name">Device name.</param>
        public DeviceAttribute(string id, string key, string name)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID is null or empty", "id");
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key is null or empty", "key");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name is null or empty", "name");

            ID = Guid.Parse(id);
            Key = key;
            Name = name;
            ListenCommands = true;
        }
        #endregion
    }
}
