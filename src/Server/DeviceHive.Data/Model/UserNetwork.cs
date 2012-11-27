using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DeviceHive.Data.Model
{
    /// <summary>
    /// Represents a user/network association
    /// </summary>
    public class UserNetwork
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public UserNetwork()
        {
        }

        /// <summary>
        /// Initializes all required properties
        /// </summary>
        /// <param name="user">Associated user object</param>
        /// <param name="network">Associated network object</param>
        public UserNetwork(User user, Network network)
        {
            if (user == null)
                throw new ArgumentNullException("user");
            if (network == null)
                throw new ArgumentNullException("network");

            this.User = user;
            this.UserID = user.ID;
            this.Network = network;
            this.NetworkID = network.ID;
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Association identifier.
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// Associated user identifier.
        /// </summary>
        public int UserID { get; set; }

        /// <summary>
        /// Associated user object.
        /// </summary>
        [Required]
        public User User { get; set; }

        /// <summary>
        /// Associated network identifier.
        /// </summary>
        public int NetworkID { get; set; }

        /// <summary>
        /// Associated network object.
        /// </summary>
        [Required]
        public Network Network { get; set; }

        #endregion
    }
}
