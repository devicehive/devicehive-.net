using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DeviceHive.Data.Validation;

namespace DeviceHive.Data.Model
{
    /// <summary>
    /// Represents a device notification, a unit of information dispatched from devices.
    /// </summary>
    public class DeviceNotification
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public DeviceNotification()
        {
            this.Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes all required properties
        /// </summary>
        /// <param name="notification">Notification text</param>
        /// <param name="device">Associated device object</param>
        public DeviceNotification(string notification, Device device)
        {
            if (string.IsNullOrEmpty(notification))
                throw new ArgumentException("Notification is null or empty!", "notification");
            if (device == null)
                throw new ArgumentNullException("device");

            this.Timestamp = DateTime.UtcNow;
            this.Notification = notification;
            this.Device = device;
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Notification identifier.
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// Notification timestamp (UTC).
        /// </summary>
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// Notification name.
        /// </summary>
        [Required]
        [StringLength(128)]
        public string Notification { get; set; }

        /// <summary>
        /// Notification parameters, a JSON object with an arbitrary structure.
        /// </summary>
        [JsonField]
        public string Parameters { get; set; }

        /// <summary>
        /// Associated device identifier.
        /// </summary>
        public int DeviceID { get; set; }

        /// <summary>
        /// Associated device object.
        /// </summary>
        [Required]
        public Device Device { get; set; }

        #endregion
    }
}
