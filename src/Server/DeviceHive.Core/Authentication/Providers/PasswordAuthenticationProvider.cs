using DeviceHive.Data;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceHive.Core.Authentication.Providers
{
    /// <summary>
    /// Represents DeviceHive password authentication provider.
    /// </summary>
    public class PasswordAuthenticationProvider : AuthenticationProvider
    {
        private int _maxLoginAttempts;

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="providerConfiguration">Configuration of the current authentication provider.</param>
        /// <param name="deviceHiveConfiguration">DeviceHiveConfiguration object.</param>
        /// <param name="dataContext">DataContext object.</param>
        public PasswordAuthenticationProvider(AuthenticationProviderConfiguration providerConfiguration,
            DeviceHiveConfiguration deviceHiveConfiguration, DataContext dataContext)
            : base(providerConfiguration, deviceHiveConfiguration, dataContext)
        {
            _maxLoginAttempts = deviceHiveConfiguration.Authentication.MaxLoginAttempts;
        }
        #endregion

        #region AuthenticationProvider Members

        /// <summary>
        /// Authenticates a user.
        /// Throws AuthenticationException in case of authentication failure.
        /// </summary>
        /// <param name="request">Request with user credentials.</param>
        /// <returns>Authenticated user.</returns>
        public override Task<User> Authenticate(JObject request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            var login = (string)request["login"];
            var password = (string)request["password"];
            if (login == null || password == null)
                throw new AuthenticationException("Login or password were not provided in the request object!");

            var user = DataContext.User.Get(login);
            if (user == null || user.Status != (int)UserStatus.Active || !user.HasPassword())
                throw new AuthenticationException("Invalid login, or user is not active, or user has no password!");

            if (!user.IsValidPassword(password))
            {
                // invalid password: increase login attemps and lockout if necessary
                IncrementUserLoginAttempts(user);
                throw new AuthenticationException("Invalid password!");
            }

            // reset login attempts, update last login and succeed
            ResetUserLoginAttempts(user);
            UpdateUserLastLogin(user);
            return Task.FromResult(user);
        }
        #endregion

        #region Private Methods

        private void IncrementUserLoginAttempts(User user)
        {
            user.LoginAttempts++;
            if (_maxLoginAttempts > 0 && user.LoginAttempts >= _maxLoginAttempts)
                user.Status = (int)UserStatus.LockedOut;
            DataContext.User.Save(user);
        }

        private void ResetUserLoginAttempts(User user)
        {
            if (user.LoginAttempts > 0)
            {
                user.LoginAttempts = 0;
                DataContext.User.Save(user);
            }
        }
        #endregion
    }
}
