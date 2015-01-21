using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DeviceHive.Data.Model
{
    /// <summary>
    /// Represents enumeration of OAuth grant types
    /// </summary>
    public enum OAuthGrantType
    {
        /// <summary>
        /// Authorization code grant type
        /// </summary>
        Code = 0,

        /// <summary>
        /// Token grant type
        /// </summary>
        Token = 1,

        /// <summary>
        /// Password grant type
        /// </summary>
        Password = 2,
    }

    /// <summary>
    /// Represents enumeration of OAuth grant access types
    /// </summary>
    public enum OAuthGrantAccessType
    {
        /// <summary>
        /// Online access, i.e. grants access for limited period of time
        /// </summary>
        Online = 0,

        /// <summary>
        /// Offline access, i.e. grants access for unlimited period of time
        /// </summary>
        Offline = 1,
    }

    /// <summary>
    /// Represents an OAuth permission grant.
    /// </summary>
    public class OAuthGrant
    {
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public OAuthGrant()
        {
        }

        /// <summary>
        /// Initializes all required properties.
        /// </summary>
        /// <param name="client">Associated OAuthClient object.</param>
        /// <param name="userId">Associated user identifier.</param>
        /// <param name="accessKey">Associated AccessKey object.</param>
        /// <param name="type">OAuth type.</param>
        /// <param name="scope">Requested OAuth scope.</param>
        public OAuthGrant(OAuthClient client, int userId, AccessKey accessKey, int type, string scope)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            if (accessKey == null)
                throw new ArgumentNullException("accessKey");
            if (string.IsNullOrEmpty(scope))
                throw new ArgumentException("Scope is null or empty!", "scope");

            this.Timestamp = DateTime.UtcNow;
            this.Client = client;
            this.UserID = userId;
            this.AccessKey = accessKey;
            this.Type = type;
            this.Scope = scope;
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// OAuth grant identifier.
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// OAuth grant timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// OAuth authorization code.
        /// </summary>
        public Guid? AuthCode { get; set; }

        /// <summary>
        /// Associated OAuthClient identifier.
        /// </summary>
        public int ClientID { get; set; }

        /// <summary>
        /// Associated OAuthClient object.
        /// </summary>
        [Required]
        public OAuthClient Client { get; set; }

        /// <summary>
        /// Associated User identifier.
        /// </summary>
        public int UserID { get; set; }

        /// <summary>
        /// Associated AccessKey identifier.
        /// </summary>
        public int AccessKeyID { get; set; }

        /// <summary>
        /// Associated AccessKey object.
        /// </summary>
        [Required]
        public AccessKey AccessKey { get; set; }

        /// <summary>
        /// OAuth grant type.
        /// <list type="bullet">
        ///     <item><description>Code: Authorization Code grant</description></item>
        ///     <item><description>Token: Implicit grant</description></item>
        ///     <item><description>Password: Password Credentials grant</description></item>
        /// </list>
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// Grant access type.
        /// Available values:
        /// <list type="bullet">
        ///     <item><description>Online: Access is requested to a limited period of time</description></item>
        ///     <item><description>Offline: Assess is requested for an unlimited period of time</description></item>
        /// </list>
        /// </summary>
        [DefaultValue(0)]
        public int AccessType { get; set; }

        /// <summary>
        /// OAuth redirect URI specified during authorization.
        /// </summary>
        [StringLength(128)]
        public string RedirectUri { get; set; }

        /// <summary>
        /// Requested OAuth scope.
        /// The scope should include a space-delimited list of required access key permissions.
        /// </summary>
        [Required]
        [StringLength(256)]
        public string Scope { get; set; }

        /// <summary>
        /// A collection of network identifiers requested for access.
        /// Set to null to request access for all accessible networks.
        /// </summary>
        public int[] Networks { get; set; }

        /// <summary>
        /// A comma-separated list of network identifiers requested for access.
        /// Used for EF serialization only.
        /// </summary>
        [StringLength(128)]
        [RegularExpression(@"^[\d\,]+$")]
        public string NetworkList
        {
            get { return Networks == null || Networks.Length == 0 ? null : string.Join(",", Networks); }
            set { Networks = value == null ? null : value.Split(',').Select(v => int.Parse(v)).ToArray(); }
        }
        #endregion
    }

    /// <summary>
    /// Represents a OAuth permission grant filter.
    /// </summary>
    public class OAuthGrantFilter
    {
        #region Public Properties

        /// <summary>
        /// Filter by grant start timestamp (UTC).
        /// </summary>
        public DateTime? Start { get; set; }

        /// <summary>
        /// Filter by grant end timestamp (UTC).
        /// </summary>
        public DateTime? End { get; set; }

        /// <summary>
        /// Filter by OAuth client identifier.
        /// </summary>
        public int? ClientID { get; set; }

        /// <summary>
        /// Filter by OAuth client OAuth identifier.
        /// </summary>
        public string ClientOAuthID { get; set; }

        /// <summary>
        /// Filter by associated access key identifier.
        /// </summary>
        public int? AccessKeyID { get; set; }

        /// <summary>
        /// Filter by OAuth grant type.
        /// </summary>
        public int? Type { get; set; }

        /// <summary>
        /// Filter by OAuth scope.
        /// </summary>
        public string Scope { get; set; }

        /// <summary>
        /// Filter by OAuth redirect URI.
        /// </summary>
        public string RedirectUri { get; set; }

        /// <summary>
        /// Filter by access type.
        /// </summary>
        public int? AccessType { get; set; }

        /// <summary>
        /// Result list sort field. Available values are Timestamp (default).
        /// </summary>
        [DefaultValue(OAuthGrantSortField.None)]
        public OAuthGrantSortField SortField { get; set; }

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

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public OAuthGrantFilter()
        {
            SortField = OAuthGrantSortField.Timestamp;
        }
        #endregion
    }

    /// <summary>
    /// Represents OAuth permission grant sort fields.
    /// </summary>
    public enum OAuthGrantSortField
    {
        None = 0,
        Timestamp = 1,
    }
}
