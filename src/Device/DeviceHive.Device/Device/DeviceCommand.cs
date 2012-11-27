using System;
using System.Collections.Generic;

namespace DeviceHive.Device
{
    /// <summary>
    /// Represents a command sent to <see cref="DeviceBase"/> descendants.
    /// </summary>
    /// <remarks>
    /// This is the generic class which uses a dictionary to hold command parameters.
    /// Alternatively, declare a custom strongly-typed class and use <see cref="ParameterAttribute"/> to specify command parameters.
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
        public Dictionary<string, object> Parameters { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes device command name and parameters.
        /// </summary>
        /// <param name="name">Command name.</param>
        /// <param name="parameters">Dictionary of command parameters.</param>
        public DeviceCommand(string name, Dictionary<string, object> parameters)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name is null or empty!", "name");

            Name = name;
            Parameters = parameters ?? new Dictionary<string, object>();
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a value of command parameter with specified name.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <returns>Parameter value.</returns>
        public object GetParameter(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            object value = null;
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
            return TypeConverter.FromObject<TValue>(GetParameter(name));
        }
        #endregion
    }
}
