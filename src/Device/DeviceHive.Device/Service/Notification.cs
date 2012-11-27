using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeviceHive.Device
{
    /// <summary>
    /// Represents a DeviceHive notification.
    /// Notifications are sent by devices to clients.
    /// </summary>
    public class Notification
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets notification identifier (server-assigned).
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets notification timestamp (server-assigned).
        /// </summary>
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets notification name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets notification parameters.
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; }

        #endregion

        #region Constructor
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Notification()
        {
        }

        /// <summary>
        /// Initializes all notification properties.
        /// </summary>
        /// <param name="name">Notification name.</param>
        /// <param name="parameters">Notification parameters.</param>
        public Notification(string name, Dictionary<string, string> parameters)
        {
            Name = name;
            Parameters = parameters;
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

            if (Parameters == null)
                Parameters = new Dictionary<string, string>();

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

            if (Parameters == null)
                return null;

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
