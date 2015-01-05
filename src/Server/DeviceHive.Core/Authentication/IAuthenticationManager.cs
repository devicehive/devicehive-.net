using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;
using Ninject;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeviceHive.Core.Authentication
{
    /// <summary>
    /// Represents interface for authentication managers.
    /// </summary>
    public interface IAuthenticationManager
    {
        /// <summary>
        /// Initializes the authentication manager.
        /// Reads the configiration and instantiates enabled authentication providers.
        /// </summary>
        /// <param name="kernel">NInject kernel.</param>
        void Initialize(IKernel kernel);

        /// <summary>
        /// Gets a list of registered authentication providers.
        /// </summary>
        /// <returns>List of <see cref="AuthenticationProviderInfo"/> objects.</returns>
        IList<AuthenticationProviderInfo> GetProviders();

        /// <summary>
        /// Authenticates a user agains the specified provider.
        /// Throws AuthenticationException in case of authentication failure.
        /// </summary>
        /// <param name="providerName">Authentication provider name.</param>
        /// <param name="request">Request object with user credentials.</param>
        /// <returns>Authenticated user.</returns>
        Task<User> AuthenticateAsync(string providerName, JObject request);

        /// <summary>
        /// Authenticates a user agains the password provider.
        /// Throws AuthenticationException in case of authentication failure.
        /// </summary>
        /// <param name="login">User login.</param>
        /// <param name="password">User password.</param>
        /// <returns>Authenticated user.</returns>
        Task<User> AuthenticateByPasswordAsync(string login, string password);

        /// <summary>
        /// Authenticates a user agains the password provider.
        /// Throws AuthenticationException in case of authentication failure.
        /// </summary>
        /// <param name="login">User login.</param>
        /// <param name="password">User password.</param>
        /// <returns>Authenticated user.</returns>
        User AuthenticateByPassword(string login, string password);
    }
}
