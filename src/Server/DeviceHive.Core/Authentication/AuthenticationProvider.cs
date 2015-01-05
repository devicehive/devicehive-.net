using DeviceHive.Data;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace DeviceHive.Core.Authentication
{
    /// <summary>
    /// Represents base class for authentication providers.
    /// </summary>
    public abstract class AuthenticationProvider
    {
        #region Protected Properties

        /// <summary>
        /// Gets configuration of the current authentication provider.
        /// </summary>
        protected AuthenticationProviderConfiguration ProviderConfiguration { get; private set; }

        /// <summary>
        /// Gets DeviceHiveConfiguration object.
        /// </summary>
        protected DeviceHiveConfiguration DeviceHiveConfiguration { get; private set; }

        /// <summary>
        /// Gets DataContext object.
        /// </summary>
        protected DataContext DataContext { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="providerConfiguration">Configuration of the current authentication provider.</param>
        /// <param name="deviceHiveConfiguration">DeviceHiveConfiguration object.</param>
        /// <param name="dataContext">DataContext object.</param>
        public AuthenticationProvider(AuthenticationProviderConfiguration providerConfiguration,
            DeviceHiveConfiguration deviceHiveConfiguration, DataContext dataContext)
        {
            if (providerConfiguration == null)
                throw new ArgumentNullException("providerConfiguration");
            if (deviceHiveConfiguration == null)
                throw new ArgumentNullException("deviceHiveConfiguration");
            if (dataContext == null)
                throw new ArgumentNullException("dataContext");

            ProviderConfiguration = providerConfiguration;
            DeviceHiveConfiguration = deviceHiveConfiguration;
            DataContext = dataContext;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Authenticates a user.
        /// Throws AuthenticationException in case of authentication failure.
        /// </summary>
        /// <param name="request">Request with user credentials.</param>
        /// <returns>Authenticated user.</returns>
        public abstract Task<User> Authenticate(JObject request);

        #endregion

        #region Protected Methods

        /// <summary>
        /// Updates user last login timestamp.
        /// </summary>
        /// <param name="user">User object to update last login timestamp for.</param>
        protected virtual void UpdateUserLastLogin(User user)
        {
            // update LastLogin only if it's too far behind - save database resources
            if (user.LastLogin == null || user.LastLogin.Value.AddHours(1) < DateTime.UtcNow)
            {
                user.LastLogin = DateTime.UtcNow;
                DataContext.User.Save(user);
            }
        }
        #endregion
    }
}
