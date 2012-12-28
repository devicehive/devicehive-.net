using System;
using Newtonsoft.Json.Linq;

namespace DeviceHive.Client
{
	/// <summary>
	/// Represents information about the current state of particular device equipment.
    /// The DeviceHive service tracks the state of device equipment if device sends "equipment" notifications.
	/// </summary>
	public class DeviceEquipmentState
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets equipment code.
        /// </summary>
        public string Id { get; set; }

		/// <summary>
		/// Gets equipment state timestamp.
		/// </summary>
		public DateTime? Timestamp { get; set; }

		/// <summary>
		/// Gets equipment state parameters.
		/// </summary>
		public JToken Parameters { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a value of equipment state parameter with specified name.
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
