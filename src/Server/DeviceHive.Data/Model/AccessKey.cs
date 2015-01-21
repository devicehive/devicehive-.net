using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;

namespace DeviceHive.Data.Model
{
    /// <summary>
    /// Represents enumeration of user roles
    /// </summary>
    public enum AccessKeyType
    {
        /// <summary>
        /// Default access key
        /// </summary>
        Default = 0,

        /// <summary>
        /// Session access key
        /// </summary>
        Session = 1,

        /// <summary>
        /// OAuth access key
        /// </summary>
        OAuth = 2,
    }

    /// <summary>
    /// Represents an access key to this API.
    /// </summary>
    public class AccessKey
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public AccessKey()
        {
        }

        /// <summary>
        /// Initializes all required properties
        /// </summary>
        /// <param name="userId">Identifier of associated user</param>
        /// <param name="type">Access key type</param>
        /// <param name="label">Access key label</param>
        public AccessKey(int userId, AccessKeyType type, string label)
        {
            if (string.IsNullOrEmpty(label))
                throw new ArgumentException("Label is null or empty!", "label");

            this.UserID = userId;
            this.Type = (int)type;
            this.Label = label;
            this.GenerateKey();
            this.Permissions = new List<AccessKeyPermission>();
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Access key identifier.
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// Associated user identifier.
        /// </summary>
        public int UserID { get; set; }

        /// <summary>
        /// Access key type.
        /// Available values:
        /// <list type="bullet">
        ///     <item><description>0: Default</description></item>
        ///     <item><description>1: Session (with sliding expiration)</description></item>
        ///     <item><description>2: OAuth (issued via OAuth2 token endpoint) </description></item>
        /// </list>
        /// </summary>
        [DefaultValue(0)]
        public int Type { get; set; }

        /// <summary>
        /// Access key label.
        /// </summary>
        [Required]
        [StringLength(64)]
        public string Label { get; set; }

        /// <summary>
        /// Access key value.
        /// </summary>
        [Required]
        [StringLength(48)]
        public string Key { get; private set; }

        /// <summary>
        /// Expiration date (UTC).
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// A collection of associated permission objects.
        /// </summary>
        public List<AccessKeyPermission> Permissions { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates a new key.
        /// </summary>
        public void GenerateKey()
        {
            var key = new byte[32];
            RNGCryptoServiceProvider.Create().GetBytes(key);
            
            this.Key = Convert.ToBase64String(key);
        }
        #endregion
    }

    /// <summary>
    /// Represents an access key filter.
    /// </summary>
    public class AccessKeyFilter
    {
        #region Public Properties

        /// <summary>
        /// Filter by access key label.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Filter by access key label pattern.
        /// </summary>
        public string LabelPattern { get; set; }

        /// <summary>
        /// Filter by acess key type.
        /// </summary>
        public int? Type { get; set; }

        /// <summary>
        /// Result list sort field. Available values are ID and Label.
        /// </summary>
        [DefaultValue(AccessKeySortField.None)]
        public AccessKeySortField SortField { get; set; }

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
    public enum AccessKeySortField
    {
        None = 0,
        ID = 1,
        Label = 2
    }
}
