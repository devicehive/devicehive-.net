using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DeviceHive.Data.Model
{
    /// <summary>
    /// Represents a network, an isolated area where devices reside.
    /// </summary>
    public class Network
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public Network()
        {
        }

        /// <summary>
        /// Initializes all required properties
        /// </summary>
        /// <param name="name">Network name</param>
        public Network(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name is null or empty!", "name");

            this.Name = name;
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Network identifier.
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// Optional key that is used to protect the network from unauthorized device registrations.
        /// When defined, devices will need to pass the key in order to register to the current network.
        /// </summary>
        [StringLength(64)]
        public string Key { get; set; }

        /// <summary>
        /// Network display name.
        /// </summary>
        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        /// <summary>
        /// Network description.
        /// </summary>
        [StringLength(128)]
        public string Description { get; set; }

        #endregion
    }

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
        /// Result list sort field. Available values are ID and Name.
        /// </summary>
        [DefaultValue(NetworkSortField.None)]
        public NetworkSortField SortField { get; set; }

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
        None = 0,
        ID = 1,
        Name = 2
    }
}
