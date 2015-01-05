namespace DeviceHive.Core.Authentication
{
    /// <summary>
    /// Represents information about authentication provider.
    /// </summary>
    public class AuthenticationProviderInfo
    {
        #region Public Properties

        /// <summary>
        /// Gets authentication provider name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets authentication provider instance.
        /// </summary>
        public AuthenticationProvider Provider { get; private set; }

        /// <summary>
        /// Gets authentication provider configuration.
        /// </summary>
        public AuthenticationProviderConfiguration Configuration { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">Authentication provider name.</param>
        /// <param name="provider">Authentication provider instance.</param>
        /// <param name="configuration">Authentication provider configuration.</param>
        internal AuthenticationProviderInfo(string name, AuthenticationProvider provider, AuthenticationProviderConfiguration configuration)
        {
            Name = name;
            Provider = provider;
            Configuration = configuration;
        }
        #endregion
    }
}
