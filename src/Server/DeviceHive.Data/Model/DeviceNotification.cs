using System;
using System.Collections.Generic;
using System.ComponentModel;
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

    /// <summary>
    /// Represents a device notification filter.
    /// </summary>
    public class DeviceNotificationFilter
    {
        #region Public Properties

        /// <summary>
        /// Filter by notification start timestamp (inclusive, UTC).
        /// </summary>
        public DateTime? Start { get; set; }

        /// <summary>
        /// Filter by notification end timestamp (inclusive, UTC).
        /// </summary>
        public DateTime? End { get; set; }

        /// <summary>
        /// Filter by notification name.
        /// </summary>
        public string Notification { get; set; }

        /// <summary>
        /// Result list sort field. Available values are Timestamp (default) and Notification.
        /// </summary>
        [DefaultValue(DeviceNotificationSortField.Timestamp)]
        public DeviceNotificationSortField SortField { get; set; }

        /// <summary>
        /// Result list sort order. Available values are ASC and DESC.
        /// </summary>
        [DefaultValue(SortOrder.ASC)]
        public SortOrder SortOrder { get; set; }

        /// <summary>
        /// Number of records to skip from the result list.
        /// </summary>
        public int? Skip { get; set; }

        /// <summary>
        /// Number of records to take from the result list (default is 1000).
        /// </summary>
        public int? Take { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DeviceNotificationFilter()
        {
            SortField = DeviceNotificationSortField.Timestamp;
            Take = 1000;
        }
        #endregion
    }

    /// <summary>
    /// Represents device notification sort fields.
    /// </summary>
    public enum DeviceNotificationSortField
    {
        None = 0,
        Timestamp = 1,
        Notification = 2
    }
}
