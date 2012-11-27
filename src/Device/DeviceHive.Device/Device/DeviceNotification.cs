using System;
using System.Collections.Generic;

namespace DeviceHive.Device
{
    /// <summary>
    /// Represents a notification sent from <see cref="DeviceBase"/> descendants.
    /// </summary>
    /// <remarks>
    /// This is the base class for notifications sent by devices.
    /// Derive from this or <see cref="DeviceEquipmentNotification"/> class to define strongly typed properties
    /// that use <see cref="Parameter"/> and <see cref="GetParameter"/> methods to access the parameters dictionary.
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
        public Dictionary<string, string> Parameters { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initialized device notification name.
        /// </summary>
        /// <param name="name">Notification name.</param>
        public DeviceNotification(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name is null or empty!", "name");

            Name = name;
            Parameters = new Dictionary<string, string>();
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Sets a value of notification parameter with specified name.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value to set.</param>
        public void Parameter(string name, string value)
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
            string stringValue = null;
            if (value != null)
            {
                if (typeof(TValue) == typeof(byte[]))
                {
                    stringValue = Convert.ToBase64String(value as byte[]);
                }
                else
                {
                    stringValue = value.ToString();
                }
            }
            Parameter(name, stringValue);
        }

        /// <summary>
        /// Gets a value of notification parameter with specified name.
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
        /// Gets a value of notification parameter with specified name.
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
