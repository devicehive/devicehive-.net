using DeviceHive.Data;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DeviceHive.Core.Authentication.Providers
{
    /// <summary>
    /// Represents Google OAuth authentication provider.
    /// </summary>
    public class GoogleAuthenticationProvider : AuthenticationProvider
    {
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="providerConfiguration">Configuration of the current authentication provider.</param>
        /// <param name="deviceHiveConfiguration">DeviceHiveConfiguration object.</param>
        /// <param name="dataContext">DataContext object.</param>
        public GoogleAuthenticationProvider(AuthenticationProviderConfiguration providerConfiguration,
            DeviceHiveConfiguration deviceHiveConfiguration, DataContext dataContext)
            : base(providerConfiguration, deviceHiveConfiguration, dataContext)
        {
            if (string.IsNullOrEmpty(providerConfiguration.ClientId) || string.IsNullOrEmpty(providerConfiguration.ClientSecret))
                throw new ArgumentException("GoogleAuthenticationProvider: please specify clientId and clientSecret in the configuration!");
        }
        #endregion

        #region AuthenticationProvider Members

        /// <summary>
        /// Authenticates a user.
        /// Throws AuthenticationException in case of authentication failure.
        /// </summary>
        /// <param name="request">Request with user credentials.</param>
        /// <returns>Authenticated user.</returns>
        public override async Task<User> AuthenticateAsync(JObject request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            var code = (string)request["code"];
            var token = (string)request["access_token"];
            if (code == null && token == null)
                throw new AuthenticationException("OAuth authentication code or token were not provided in the request object!");

            var client = new HttpClient { BaseAddress = new Uri("https://www.googleapis.com") };

            if (token == null)
            {
                // exchange auth code to the access token
                var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "client_id", ProviderConfiguration.ClientId },
                    { "client_secret", ProviderConfiguration.ClientSecret },
                    { "redirect_uri", DeviceHiveConfiguration.Authentication.OAuthRedirectUri },
                    { "grant_type", "authorization_code" },
                    { "code", code },
                });

                var tokenResponse = await client.PostAsync("oauth2/v3/token", tokenRequest);
                if (!tokenResponse.IsSuccessStatusCode)
                    throw new AuthenticationException("Invalid authentication code!");

                var tokenResponseJson = JObject.Parse(await tokenResponse.Content.ReadAsStringAsync());
                token = (string)tokenResponseJson["access_token"];
            }
            else
            {
                // verify that token was issued to the current application
                var tokenResponse = await client.GetAsync("oauth2/v1/tokeninfo?access_token=" + token);
                if (!tokenResponse.IsSuccessStatusCode)
                    throw new AuthenticationException("Invalid access token!");

                var tokenResponseJson = JObject.Parse(await tokenResponse.Content.ReadAsStringAsync());
                if ((string)tokenResponseJson["issued_to"] != ProviderConfiguration.ClientId)
                    throw new AuthenticationException("The token was not issued specifically to the current application!");
            }

            // get user Google login
            var meResponse = await client.GetAsync("plus/v1/people/me?access_token=" + token);
            if (!meResponse.IsSuccessStatusCode)
                throw new AuthenticationException("Invalid access token!");

            var meResponseJson = JObject.Parse(await meResponse.Content.ReadAsStringAsync());
            var accountEmail = ((JArray)meResponseJson["emails"]).FirstOrDefault(e => (string)e["type"] == "account");
            if (accountEmail == null)
                throw new AuthenticationException("Google account does not have any account emails associated with it!");
            var login = (string)accountEmail["value"];
                
            // get user from database
            var user = DataContext.User.GetByGoogleLogin(login);
            if (user == null || user.Status != (int)UserStatus.Active)
                throw new AuthenticationException("Unknown Google login, or user is not active!");

            UpdateUserLastLogin(user);
            return user; // success

            throw new AuthenticationException("Access token was not provided in the request object!");
        }
        #endregion
    }
}
