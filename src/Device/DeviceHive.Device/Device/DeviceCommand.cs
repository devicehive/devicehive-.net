using System;
using System.Collections.Generic;

namespace DeviceHive.Device
{
    /// <summary>
    /// Represents a notification sent to <see cref="DeviceBase"/> descendants.
    /// </summary>
    /// <remarks>
    /// This is the base class for commands received by devices.
    /// Derive from this class to define strongly typed properties
    /// that use <see cref="GetParameter"/> method to read from the parameters dictionary.
    /// </remarks>
    public class DeviceCommand
    {
        #region Public Properties

        /// <summary>
        /// Gets command name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets a dictionary of command parameters.
        /// </summary>
        public Dictionary<string, string> Parameters { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes device command name and parameters.
        /// </summary>
        /// <param name="name">Command name.</param>
        /// <param name="parameters">Dictionary of command parameters.</param>
        public DeviceCommand(string name, Dictionary<string, string> parameters)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name is null or empty!", "name");

            Name = name;
            Parameters = parameters ?? new Dictionary<string, string>();
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a value of command parameter with specified name.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <returns>Parameter value.</returns>
        public string GetParameter(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            string value = null;
            Parameters.TryGetValue(name, out value);
            return value;
        }

        /// <summary>
        /// Gets a value of command parameter with specified name.
        /// </summary>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="name">Parameter name.</param>
        /// <returns>Parameter value.</returns>
        public TValue GetParameter<TValue>(string name)
        {
            string stringValue = GetParameter(name);
            if (stringValue == null)
            {
                return default(TValue);
            }
            if (typeof(TValue) == typeof(byte[]))
            {
                return (TValue)(object)Convert.FromBase64String(stringValue);
            }
            return TypeConverter.Parse<TValue>(stringValue);
        }
        #endregion
    }
}
