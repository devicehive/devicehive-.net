using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;
using Ninject;
using Ninject.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeviceHive.Core.Authentication
{
    /// <summary>
    /// Represents default implementation of <see cref="IAuthenticationManager" />.
    /// </summary>
    public class AuthenticationManager : IAuthenticationManager
    {
        private readonly DeviceHiveConfiguration _configuration;
        private readonly Dictionary<string, AuthenticationProviderInfo> _providers;

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="configuration">DeviceHive configuration</param>
        public AuthenticationManager(DeviceHiveConfiguration configuration)
        {
            _configuration = configuration;
            _providers = new Dictionary<string, AuthenticationProviderInfo>();
        }
        #endregion

        #region IAuthenticationManager Members

        /// <summary>
        /// Initializes the authentication manager.
        /// Reads the configiration and instantiates enabled authentication providers.
        /// </summary>
        /// <param name="kernel">NInject kernel.</param>
        public void Initialize(IKernel kernel)
        {
            var providers = _configuration.Authentication.Providers.Cast<AuthenticationProviderConfigurationElement>().ToArray();
            foreach (var provider in providers.Where(p => p.Enabled))
            {
                var providerType = Type.GetType(provider.Type, false);
                if (providerType == null)
                {
                    throw new Exception(string.Format("Could not load type: '{0}'!" +
                        " Please put the all referenced assemblies into the DeviceHive executable folder.", provider.Type));
                }
                if (!typeof(AuthenticationProvider).IsAssignableFrom(providerType))
                {
                    throw new Exception(string.Format("The type '{0}' must implement AuthenticationProvider" +
                        " in order to be registered as authentication provider!", providerType));
                }

                var providerConfiguration = new AuthenticationProviderConfiguration(provider.ClientId, provider.ClientSecret, provider.Argument);
                var providerInstance = (AuthenticationProvider)kernel.Get(providerType, new ConstructorArgument("providerConfiguration", providerConfiguration));
                var providerInfo = new AuthenticationProviderInfo(provider.Name, providerInstance, providerConfiguration);
                _providers.Add(provider.Name, providerInfo);
            }
        }

        /// <summary>
        /// Gets a list of registered authentication providers.
        /// </summary>
        /// <returns>List of <see cref="AuthenticationProviderInfo"/> objects.</returns>
        public IList<AuthenticationProviderInfo> GetProviders()
        {
            return _providers.Values.ToList();
        }

        /// <summary>
        /// Authenticates a user agains the specified provider.
        /// Throws AuthenticationException in case of authentication failure.
        /// </summary>
        /// <param name="providerName">Authentication provider name.</param>
        /// <param name="request">Request object with user credentials.</param>
        /// <returns>Authenticated user.</returns>
        public async Task<User> AuthenticateAsync(string providerName, JObject request)
        {
            if (string.IsNullOrEmpty(providerName))
                throw new ArgumentException("Provider name is null or empty!");
            if (request == null)
                throw new ArgumentNullException("request");

            if (!_providers.ContainsKey(providerName))
                throw new AuthenticationException(string.Format("Authentication provider with name '{0}' was not registered or was disabled!", providerName));

            var providerInfo = _providers[providerName];
            var user = await providerInfo.Provider.AuthenticateAsync(request);
            if (user == null)
                throw new AuthenticationException("Authentication provider did not return a user object!");

            return user;
        }

        /// <summary>
        /// Authenticates a user agains the password provider.
        /// Throws AuthenticationException in case of authentication failure.
        /// </summary>
        /// <param name="login">User login.</param>
        /// <param name="password">User password.</param>
        /// <returns>Authenticated user.</returns>
        public async Task<User> AuthenticateByPasswordAsync(string login, string password)
        {
            if (string.IsNullOrEmpty(login))
                throw new ArgumentException("Login is null or empty!");
            if (password == null)
                throw new ArgumentNullException("password");

            var providerName = _configuration.Authentication.PasswordProviderName;
            if (string.IsNullOrEmpty(providerName))
                throw new AuthenticationException("Provider for password authentication was not configured!");

            var request = new JObject(
                new JProperty("login", login),
                new JProperty("password", password));
            return await AuthenticateAsync(providerName, request);

        }

        /// <summary>
        /// Authenticates a user agains the password provider.
        /// Throws AuthenticationException in case of authentication failure.
        /// </summary>
        /// <param name="login">User login.</param>
        /// <param name="password">User password.</param>
        /// <returns>Authenticated user.</returns>
        public User AuthenticateByPassword(string login, string password)
        {
            try
            {
                return AuthenticateByPasswordAsync(login, password).Result;
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
        }
        #endregion
    }
}
