using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DeviceHive.Data.Model
{
    /// <summary>
    /// Represents a client with the access to the DeviceHive OAuth API.
    /// </summary>
    public class OAuthClient
    {
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public OAuthClient()
        {
        }

        /// <summary>
        /// Initializes all required properties.
        /// </summary>
        /// <param name="name">Client display name.</param>
        /// <param name="domain">Client domain.</param>
        /// <param name="oauthId">Client OAuth identifier.</param>
        public OAuthClient(string name, string domain, string oauthId)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name is null or empty!", "name");
            if (string.IsNullOrEmpty(domain))
                throw new ArgumentException("Domain is null or empty!", "domain");
            if (string.IsNullOrEmpty(oauthId))
                throw new ArgumentException("OAuthID is null or empty!", "oauthId");

            this.Name = name;
            this.Domain = domain;
            this.OAuthID = oauthId;

            // generate random 24-characters OAuth secret
            var buffer = new byte[18];
            new Random().NextBytes(buffer);
            OAuthSecret = Convert.ToBase64String(buffer);
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Client identifier.
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// Client display name.
        /// </summary>
        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        /// <summary>
        /// Client domain.
        /// </summary>
        [Required]
        [StringLength(128)]
        public string Domain { get; set; }

        /// <summary>
        /// Client IP subnet.
        /// </summary>
        [StringLength(128)]
        public string Subnet { get; set; }

        /// <summary>
        /// Client OAuth ID.
        /// </summary>
        [Required]
        [StringLength(32)]
        public string OAuthID { get; set; }

        /// <summary>
        /// Client OAuth secret.
        /// </summary>
        [Required]
        [StringLength(32)]
        public string OAuthSecret { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents a OAuth client filter.
    /// </summary>
    public class OAuthClientFilter
    {
        #region Public Properties

        /// <summary>
        /// Filter by client name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Filter by client name pattern.
        /// </summary>
        public string NamePattern { get; set; }

        /// <summary>
        /// Filter by domain.
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Filter by OAuth client ID.
        /// </summary>
        public string OAuthID { get; set; }

        /// <summary>
        /// Result list sort field. Available values are ID, Name, Domain and OAuthID.
        /// </summary>
        [DefaultValue(OAuthClientSortField.None)]
        public OAuthClientSortField SortField { get; set; }

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
    /// Represents OAuth client sort fields.
    /// </summary>
    public enum OAuthClientSortField
    {
        None = 0,
        ID = 1,
        Name = 2,
        Domain = 3,
        OAuthID = 4,
    }
}
