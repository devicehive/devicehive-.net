using System;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents a notification filter.
    /// </summary>
    public class NotificationFilter
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
        /// Result list sort field.
        /// </summary>
        public DeviceNotificationSortField SortField { get; set; }

        /// <summary>
        /// Result list sort order.
        /// </summary>
        public SortOrder SortOrder { get; set; }

        /// <summary>
        /// Number of records to skip from the result list.
        /// </summary>
        public int? Skip { get; set; }

        /// <summary>
        /// Number of records to take from the result list.
        /// </summary>
        public int? Take { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents device notification sort fields.
    /// </summary>
    public enum DeviceNotificationSortField
    {
        /// <summary>
        /// Sort by timestamp.
        /// </summary>
        Timestamp = 0,

        /// <summary>
        /// Sort by notification name, then by timestamp.
        /// </summary>
        Notification = 1
    }
}
