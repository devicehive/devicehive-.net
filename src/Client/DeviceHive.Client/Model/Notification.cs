using System;
using Newtonsoft.Json.Linq;

namespace DeviceHive.Client
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
        /// Gets or sets device unique identifier (server-assigned).
        /// </summary>
        public string DeviceGuid { get; set; }

        /// <summary>
        /// Gets or sets notification name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets notification parameters.
        /// </summary>
        public JToken Parameters { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Notification()
        {
        }

        /// <summary>
        /// Initializes notification name property.
        /// </summary>
        /// <param name="name">Notification name.</param>
        public Notification(string name)
            : this()
        {
            Name = name;
        }

        /// <summary>
        /// Initializes all notification properties.
        /// </summary>
        /// <param name="name">Notification name.</param>
        /// <param name="parameters">Notification parameters.</param>
        public Notification(string name, JToken parameters)
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
