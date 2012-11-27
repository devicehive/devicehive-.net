using System;

namespace DeviceHive.Device
{
    /// <summary>
    /// DeviceCommandAttribute set on descendants of the <see cref="DeviceBase"/> class to mark methods used for handing DeviceHive commands.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DeviceCommandAttribute : Attribute
    {
        #region Public Properties

        /// <summary>
        /// Gets DeviceHive command name
        /// </summary>
        public string Name { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes device command name.
        /// </summary>
        /// <param name="name">DeviceHive command name.</param>
        public DeviceCommandAttribute(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name is null or empty", "name");

            Name = name;
        }
        #endregion
    }
}
