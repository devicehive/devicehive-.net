using System;
using System.Collections.Generic;
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
        /// Default constructor
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
        /// The key maximum length is 64 characters and it must be unique across all the networks.
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
}
