using System;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents a command filter.
    /// </summary>
    public class CommandFilter
    {
        #region Public Properties

        /// <summary>
        /// Filter by command start timestamp (inclusive, UTC).
        /// </summary>
        public DateTime? Start { get; set; }

        /// <summary>
        /// Filter by command end timestamp (inclusive, UTC).
        /// </summary>
        public DateTime? End { get; set; }

        /// <summary>
        /// Filter by command name.
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// Filter by command status.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Result list sort field.
        /// </summary>
        public DeviceCommandSortField SortField { get; set; }

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
    /// Represents device command sort fields.
    /// </summary>
    public enum DeviceCommandSortField
    {
        /// <summary>
        /// Sort by timestamp.
        /// </summary>
        Timestamp = 0,

        /// <summary>
        /// Sort by command name, then by timestamp.
        /// </summary>
        Command = 1,

        /// <summary>
        /// Sort by command status, then by timestamp.
        /// </summary>
        Status = 2
    }
}
