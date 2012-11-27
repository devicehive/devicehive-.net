using System;
using System.Collections.Generic;

namespace DeviceHive.Device
{
    /// <summary>
    /// Represents a notification sent from <see cref="DeviceBase"/> descendants.
    /// </summary>
    /// <remarks>
    /// This is the generic class which uses a dictionary to hold notification parameters.
    /// Alternatively, declare a custom strongly-typed class and use <see cref="ParameterAttribute"/> to specify notification parameters.
    /// </remarks>
    public class DeviceNotification
    {
        #region Public Properties

        /// <summary>
        /// Gets notification name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets a dictionary of notification parameters.
        /// </summary>
        public Dictionary<string, object> Parameters { get; private set; }

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
            Parameters = new Dictionary<string, object>();
        }

        /// <summary>
        /// Initializes device notification name and parameters.
        /// </summary>
        /// <param name="name">Notification name.</param>
        /// <param name="parameters">Notification parameters dictionary.</param>
        public DeviceNotification(string name, Dictionary<string, object> parameters)
            : this(name)
        {
            Parameters = parameters ?? new Dictionary<string, object>();
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Sets a value of notification parameter with specified name.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value to set.</param>
        public void Parameter(string name, object value)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            Parameters[name] = value;
        }

        /// <summary>
        /// Sets a value of notification parameter with specified name.
        /// </summary>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value to set.</param>
        public void Parameter<TValue>(string name, TValue value)
        {
            Parameter(name, TypeConverter.ToObject(value));
        }

        /// <summary>
        /// Gets a value of notification parameter with specified name.
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
        /// Gets a value of notification parameter with specified name.
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
