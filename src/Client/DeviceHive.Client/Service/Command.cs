using System;
using System.Collections.Generic;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents a DeviceHive command.
    /// Commands are sent by clients to devices.
    /// </summary>
    public class Command
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets command identifier (server-assigned).
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets command timestamp (server-assigned).
        /// </summary>
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets command name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets command parameters.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; }

        /// <summary>
        /// Gets or sets command execution status.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets command result (optional).
        /// </summary>
        public string Result { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Command()
        {
        }

        /// <summary>
        /// Initializes command name property.
        /// </summary>
        /// <param name="name">Command name.</param>
        public Command(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Initializes all command properties.
        /// </summary>
        /// <param name="name">Command name.</param>
        /// <param name="parameters">Command parameters.</param>
        public Command(string name, Dictionary<string, object> parameters)
            : this(name)
        {
            Parameters = parameters;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Sets a value of command parameter with specified name.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value to set.</param>
        public void Parameter(string name, object value)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (Parameters == null)
                Parameters = new Dictionary<string, object>();

            Parameters[name] = value;
        }

        /// <summary>
        /// Sets a value of command parameter with specified name.
        /// </summary>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value to set.</param>
        public void Parameter<TValue>(string name, TValue value)
        {
            Parameter(name, TypeConverter.ToObject(value));
        }

        /// <summary>
        /// Gets a value of command parameter with specified name.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <returns>Parameter value.</returns>
        public object GetParameter(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (Parameters == null)
                return null;

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
