using System;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents a device filter.
    /// </summary>
    public class DeviceFilter
    {
        #region Public Properties

        /// <summary>
        /// Filter by device name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Filter by device name pattern.
        /// </summary>
        public string NamePattern { get; set; }

        /// <summary>
        /// Filter by device status.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Filter by associated network identifier.
        /// </summary>
        public int? NetworkId { get; set; }

        /// <summary>
        /// Filter by associated network name.
        /// </summary>
        public string NetworkName { get; set; }

        /// <summary>
        /// Filter by associated device class identifier.
        /// </summary>
        public int? DeviceClassId { get; set; }

        /// <summary>
        /// Filter by associated device class name.
        /// </summary>
        public string DeviceClassName { get; set; }

        /// <summary>
        /// Filter by associated device class version.
        /// </summary>
        public string DeviceClassVersion { get; set; }

        /// <summary>
        /// Result list sort field.
        /// </summary>
        public DeviceSortField SortField { get; set; }

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
    /// Represents device sort fields.
    /// </summary>
    public enum DeviceSortField
    {
        /// <summary>
        /// No sorting.
        /// </summary>
        None = 0,

        /// <summary>
        /// Sort by device name.
        /// </summary>
        Name = 1,

        /// <summary>
        /// Sort by device status.
        /// </summary>
        Status = 2,

        /// <summary>
        /// Sort by device network.
        /// </summary>
        Network = 3,

        /// <summary>
        /// Sort by device class.
        /// </summary>
        DeviceClass = 4
    }
}
