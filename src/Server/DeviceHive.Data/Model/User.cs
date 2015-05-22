using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DeviceHive.Data.Validation;

namespace DeviceHive.Data.Model
{
    /// <summary>
    /// Represents enumeration of user roles
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// Administrator role
        /// </summary>
        Administrator = 0,

        /// <summary>
        /// Client role
        /// </summary>
        Client = 1,
    }

    /// <summary>
    /// Represents enumeration of user statuses
    /// </summary>
    public enum UserStatus
    {
        /// <summary>
        /// The user is active
        /// </summary>
        Active = 0,

        /// <summary>
        /// The user is locked out
        /// </summary>
        LockedOut = 1,

        /// <summary>
        /// The user is disabled
        /// </summary>
        Disabled = 2,

        /// <summary>
        /// The user is deleted
        /// </summary>
        Deleted = 3,
    }

    /// <summary>
    /// Represents a user to this API.
    /// </summary>
    public class User
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public User()
        {
        }

        /// <summary>
        /// Initializes all required properties
        /// </summary>
        /// <param name="login">User login</param>
        /// <param name="role">User role</param>
        /// <param name="status">User status</param>
        public User(string login, int role, int status)
        {
            if (string.IsNullOrEmpty(login))
                throw new ArgumentException("Login is null or empty!", "login");

            this.Login = login;
            this.Role = role;
            this.Status = status;
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// User identifier.
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// User login using during authentication.
        /// </summary>
        [Required]
        [StringLength(64)]
        public string Login { get; set; }

        /// <summary>
        /// User password hash.
        /// </summary>
        [StringLength(48)]
        public string PasswordHash { get; private set; }

        /// <summary>
        /// User password salt.
        /// </summary>
        [StringLength(24)]
        public string PasswordSalt { get; private set; }

        /// <summary>
        /// User Facebook login (for OAuth authentication).
        /// </summary>
        [StringLength(64)]
        public string FacebookLogin { get; set; }

        /// <summary>
        /// User Google login (for OAuth authentication).
        /// </summary>
        [StringLength(64)]
        public string GoogleLogin { get; set; }

        /// <summary>
        /// User Github login (for OAuth authentication).
        /// </summary>
        [StringLength(64)]
        public string GithubLogin { get; set; }

        /// <summary>
        /// User role.
        /// Available values:
        /// <list type="bullet">
        ///     <item><description>0: Administrator role</description></item>
        ///     <item><description>1: Client role</description></item>
        /// </list>
        /// </summary>
        public int Role { get; set; }

        /// <summary>
        /// User status.
        /// Available values:
        /// <list type="bullet">
        ///     <item><description>0: The user is active</description></item>
        ///     <item><description>1: The user has been locked out due to invalid login attempts</description></item>
        ///     <item><description>2: The user has been disabled</description></item>
        ///     <item><description>3: The user has been deleted</description></item>
        /// </list>
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Number of user invalid login attempts.
        /// </summary>
        public int LoginAttempts { get; set; }

        /// <summary>
        /// User last login timestamp (UTC).
        /// </summary>
        public DateTime? LastLogin { get; set; }

        /// <summary>
        /// Arbitrary user data.
        /// </summary>
        [JsonField]
        public string Data { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Calculates password hash and assigns it to the current user
        /// </summary>
        /// <param name="password">New user password</param>
        public void SetPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password is null or empty!", "password");

            // generate random 24-characters salt
            var buffer = new byte[18];
            new Random().NextBytes(buffer);
            PasswordSalt = Convert.ToBase64String(buffer);

            // calculate password hash
            buffer = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(PasswordSalt + password));
            PasswordHash = Convert.ToBase64String(buffer);
        }

        /// <summary>
        /// Clears password of the current user
        /// </summary>
        public void ClearPassword()
        {
            PasswordHash = null;
            PasswordSalt = null;
        }

        /// <summary>
        /// Checks if current user has password set
        /// </summary>
        /// <returns>True if the current user has password set</returns>
        public bool HasPassword()
        {
            return PasswordHash != null;
        }

        /// <summary>
        /// Checks if passed password equals to the one assigned to the current user
        /// </summary>
        /// <param name="password">Password to check</param>
        /// <returns>True of passed password is valid</returns>
        public bool IsValidPassword(string password)
        {
            // calculate password hash
            var buffer = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(PasswordSalt + password));
            return string.Equals(PasswordHash, Convert.ToBase64String(buffer));
        }
        #endregion
    }

    /// <summary>
    /// Represents a user filter.
    /// </summary>
    public class UserFilter
    {
        #region Public Properties

        /// <summary>
        /// Filter by user login.
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Filter by user login pattern.
        /// </summary>
        public string LoginPattern { get; set; }

        /// <summary>
        /// Filter by user role. 0 is Administrator, 1 is Client.
        /// </summary>
        public int? Role { get; set; }

        /// <summary>
        /// Filter by user status. 0 is Active, 1 is Locked Out, 2 is Disabled.
        /// </summary>
        public int? Status { get; set; }

        /// <summary>
        /// Result list sort field. Available values are ID and Login.
        /// </summary>
        [DefaultValue(UserSortField.None)]
        public UserSortField SortField { get; set; }

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
    /// Represents user sort fields.
    /// </summary>
    public enum UserSortField
    {
        None = 0,
        ID = 1,
        Login = 2
    }
}
