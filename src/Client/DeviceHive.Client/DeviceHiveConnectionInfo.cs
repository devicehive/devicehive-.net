using System;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents connection information to the DeviceHive server.
    /// </summary>
    public class DeviceHiveConnectionInfo
    {
        #region Public Properties

        /// <summary>
        /// Gets DeviceHive service URL.
        /// </summary>
        public string ServiceUrl { get; private set; }

        /// <summary>
        /// Gets DeviceHive login.
        /// </summary>
        public string Login { get; private set; }

        /// <summary>
        /// Gets DeviceHive password.
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Gets DeviceHive access key.
        /// </summary>
        public string AccessKey { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes connection to DeviceHive with no credentials.
        /// </summary>
        /// <param name="serviceUrl">DeviceHive service URL.</param>
        public DeviceHiveConnectionInfo(string serviceUrl)
        {
            if (serviceUrl == null)
                throw new ArgumentNullException("serviceUrl");

            if (!Uri.IsWellFormedUriString(serviceUrl, UriKind.Absolute))
                throw new ArgumentException("Specified service URL is malformed!", "serviceUrl");

            ServiceUrl = serviceUrl;
        }

        /// <summary>
        /// Initializes connection to DeviceHive using client login/password combination.
        /// </summary>
        /// <param name="serviceUrl">DeviceHive service URL.</param>
        /// <param name="login">Client login.</param>
        /// <param name="password">Client password.</param>
        public DeviceHiveConnectionInfo(string serviceUrl, string login, string password)
            : this(serviceUrl)
        {
            if (string.IsNullOrEmpty(login))
                throw new ArgumentException("Login is null or empty!", "login");

            Login = login;
            Password = password;
        }

        /// <summary>
        /// Initializes connection to DeviceHive using access key.
        /// </summary>
        /// <param name="serviceUrl">DeviceHive service URL.</param>
        /// <param name="accessKey">Access key string.</param>
        public DeviceHiveConnectionInfo(string serviceUrl, string accessKey)
            : this(serviceUrl)
        {
            if (string.IsNullOrEmpty(accessKey))
                throw new ArgumentException("AccessKey is null or empty!", "accessKey");

            AccessKey = accessKey;
        }
        #endregion
    }
}
