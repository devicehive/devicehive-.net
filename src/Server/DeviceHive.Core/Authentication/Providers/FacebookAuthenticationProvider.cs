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
    /// Represents Facebook OAuth authentication provider.
    /// </summary>
    public class FacebookAuthenticationProvider : AuthenticationProvider
    {
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="providerConfiguration">Configuration of the current authentication provider.</param>
        /// <param name="deviceHiveConfiguration">DeviceHiveConfiguration object.</param>
        /// <param name="dataContext">DataContext object.</param>
        public FacebookAuthenticationProvider(AuthenticationProviderConfiguration providerConfiguration,
            DeviceHiveConfiguration deviceHiveConfiguration, DataContext dataContext)
            : base(providerConfiguration, deviceHiveConfiguration, dataContext)
        {
            if (string.IsNullOrEmpty(providerConfiguration.ClientId) || string.IsNullOrEmpty(providerConfiguration.ClientSecret))
                throw new ArgumentException("FacebookAuthenticationProvider: please specify clientId and clientSecret in the configuration!");
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
            var redirectUri = (string)request["redirect_uri"];
            if (code == null && token == null)
                throw new AuthenticationException("OAuth authentication code or token were not provided in the request object!");
            if (code != null && redirectUri == null)
                throw new AuthenticationException("Redirect URI was not provided in the request object!");

            var client = new HttpClient { BaseAddress = new Uri("https://graph.facebook.com") };

            if (token == null)
            {
                // exchange auth code to the access token
                var tokenRequest = new Dictionary<string, string> {
                    { "client_id", ProviderConfiguration.ClientId },
                    { "client_secret", ProviderConfiguration.ClientSecret },
                    { "redirect_uri", redirectUri },
                    { "code", code },
                };

                var tokenResponse = await client.GetAsync("oauth/access_token?" + string.Join("&",
                    tokenRequest.Select(p => string.Format("{0}={1}", p.Key, p.Value))));
                if (!tokenResponse.IsSuccessStatusCode)
                    throw new AuthenticationException("Invalid authentication code!");

                var tokenResponseString = await tokenResponse.Content.ReadAsStringAsync();
                var tokenResponseDict = tokenResponseString.Split('&').Select(s => s.Split('=')).ToDictionary(s => s[0], s => s[1]);
                token = tokenResponseDict.ContainsKey("access_token") ? tokenResponseDict["access_token"] : null;
            }
            else
            {
                // verify that token was issued to the current application
                var appResponse = await client.GetAsync("app?access_token=" + token);
                if (!appResponse.IsSuccessStatusCode)
                    throw new AuthenticationException("Invalid access token!");

                var appResponseJson = JObject.Parse(await appResponse.Content.ReadAsStringAsync());
                if ((string)appResponseJson["id"] != ProviderConfiguration.ClientId)
                    throw new AuthenticationException("The token was not issued specifically to the current application!");
            }

            // get user Facebook login
            var meResponse = await client.GetAsync("me?access_token=" + token);
            if (!meResponse.IsSuccessStatusCode)
                throw new AuthenticationException("Invalid access token!");

            var meResponseJson = JObject.Parse(await meResponse.Content.ReadAsStringAsync());
            var login = (string)meResponseJson["email"];

            // get user from database
            var user = DataContext.User.GetByFacebookLogin(login);
            if (user == null || user.Status != (int)UserStatus.Active)
                throw new AuthenticationException("Unknown Facebook login, or user is not active!");

            UpdateUserLastLogin(user);
            return user; // success
        }
        #endregion
    }
}
