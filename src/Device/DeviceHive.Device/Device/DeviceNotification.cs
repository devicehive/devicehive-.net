using System;
using Newtonsoft.Json.Linq;

namespace DeviceHive.Device
{
    /// <summary>
    /// Represents a notification sent from <see cref="DeviceBase"/> descendants.
    /// </summary>
    /// <remarks>
    /// This is the generic class which uses a dictionary to hold notification parameters.
    /// Alternatively, declare a custom strongly-typed class and use json mapping to specify notification parameters.
    /// </remarks>
    public class DeviceNotification
    {
        #region Public Properties

        /// <summary>
        /// Gets notification name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets notification parameters in the raw json format.
        /// </summary>
        public JToken Parameters { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes device notification name.
        /// </summary>
        /// <param name="name">Notification name.</param>
        public DeviceNotification(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name is null or empty!", "name");

            Name = name;
        }

        /// <summary>
        /// Initializes device notification name and parameters.
        /// </summary>
        /// <param name="name">Notification name.</param>
        /// <param name="parameters">Notification parameters in the raw json format.</param>
        public DeviceNotification(string name, JToken parameters)
            : this(name)
        {
            Parameters = parameters;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Sets a value of notification parameter with specified name.
        /// </summary>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value to set.</param>
        public void Parameter<TValue>(string name, TValue value)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (Parameters != null && Parameters.Type != JTokenType.Object)
                throw new InvalidOperationException("Parameters must be a json object to use this method!");

            if (Parameters == null)
                Parameters = new JObject();

            Parameters[name] = JToken.FromObject(value);
        }

        /// <summary>
        /// Gets a value of notification parameter with specified name.
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
