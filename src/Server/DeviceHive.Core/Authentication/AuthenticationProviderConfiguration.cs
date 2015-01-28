namespace DeviceHive.Core.Authentication
{
    /// <summary>
    /// Represents configuration of an authentication provider.
    /// </summary>
    public class AuthenticationProviderConfiguration
    {
        #region Public Properties

        /// <summary>
        /// Gets client identifier in third-party identity provider.
        /// </summary>
        public string ClientId { get; private set; }

        /// <summary>
        /// Gets client secret in third-party identity provider.
        /// </summary>
        public string ClientSecret { get; private set; }

        /// <summary>
        /// Gets additional argument in provider configuration.
        /// </summary>
        public string Argument { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="clientId">Client identifier in third-party identity provider.</param>
        /// <param name="clientSecret">Client secret in third-party identity provider.</param>
        /// <param name="argument">Additional argument in provider configuration.</param>
        public AuthenticationProviderConfiguration(string clientId, string clientSecret, string argument)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            Argument = argument;
        }
        #endregion
    }
}
