using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;

namespace DeviceHive.Data.Model
{
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
        /// <param name="label">Access key label</param>
        public AccessKey(int userId, string label)
        {
            if (string.IsNullOrEmpty(label))
                throw new ArgumentException("Label is null or empty!", "label");

            this.UserID = userId;
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
        public List<AccessKeyPermission> Permissions { get; private set; }

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
}
