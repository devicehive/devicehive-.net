using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents enumeration of user roles.
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// Administrator role.
        /// </summary>
        Administrator = 0,

        /// <summary>
        /// Client role.
        /// </summary>
        Client = 1,
    }

    /// <summary>
    /// Represents a DeviceHive user.
    /// </summary>
    public class User
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets user identifier.
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets user login.
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Gets or sets old user password (required when setting a new password).
        /// </summary>
        public string OldPassword { get; set; }

        /// <summary>
        /// Gets or sets new user password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets user role.
        /// </summary>
        public UserRole Role { get; set; }

        /// <summary>
        /// Gets or sets the list of associated networks.
        /// </summary>
        public List<UserNetwork> Networks { get; set; }

        #endregion
    }
}
