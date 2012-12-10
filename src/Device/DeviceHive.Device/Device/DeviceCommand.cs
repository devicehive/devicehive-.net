using System;
using Newtonsoft.Json.Linq;

namespace DeviceHive.Device
{
    /// <summary>
    /// Represents a command sent to <see cref="DeviceBase"/> descendants.
    /// </summary>
    /// <remarks>
    /// This is the generic class which uses a raw json token to hold command parameters.
    /// Alternatively, declare a custom strongly-typed class and use json mapping attributes to specify command parameters.
    /// </remarks>
    public class DeviceCommand
    {
        #region Public Properties

        /// <summary>
        /// Gets command name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets command parameters in the raw json format.
        /// </summary>
        public JToken Parameters { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes device command name and parameters.
        /// </summary>
        /// <param name="name">Command name.</param>
        /// <param name="parameters">Command parameters in the raw json format.</param>
        public DeviceCommand(string name, JToken parameters)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name is null or empty!", "name");

            Name = name;
            Parameters = parameters;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a value of command parameter with specified name.
        /// </summary>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="name">Parameter name.</param>
        /// <returns>Parameter value.</returns>
        public TValue GetParameter<TValue>(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (Parameters == null)
                return default(TValue);

            if (Parameters.Type != JTokenType.Object || Parameters[name] == null)
                return default(TValue);

            return Parameters[name].ToObject<TValue>();
        }
        #endregion
    }
}
