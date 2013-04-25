using System;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents a network filter.
    /// </summary>
    public class NetworkFilter
    {
        #region Public Properties

        /// <summary>
        /// Filter by network name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Filter by network name pattern.
        /// </summary>
        public string NamePattern { get; set; }

        /// <summary>
        /// Result list sort field.
        /// </summary>
        public NetworkSortField SortField { get; set; }

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
    /// Represents network sort fields.
    /// </summary>
    public enum NetworkSortField
    {
        /// <summary>
        /// No sorting.
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Sort by network identifier.
        /// </summary>
        ID = 1,

        /// <summary>
        /// Sort by network name.
        /// </summary>
        Name = 2
    }
}
