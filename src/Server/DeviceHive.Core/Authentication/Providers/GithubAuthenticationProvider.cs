using DeviceHive.Data;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DeviceHive.Core.Authentication.Providers
{
    /// <summary>
    /// Represents GitHub OAuth authentication provider.
    /// </summary>
    public class GithubAuthenticationProvider : AuthenticationProvider
    {
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="providerConfiguration">Configuration of the current authentication provider.</param>
        /// <param name="deviceHiveConfiguration">DeviceHiveConfiguration object.</param>
        /// <param name="dataContext">DataContext object.</param>
        public GithubAuthenticationProvider(AuthenticationProviderConfiguration providerConfiguration,
            DeviceHiveConfiguration deviceHiveConfiguration, DataContext dataContext)
            : base(providerConfiguration, deviceHiveConfiguration, dataContext)
        {
            if (string.IsNullOrEmpty(providerConfiguration.ClientId) || string.IsNullOrEmpty(providerConfiguration.ClientSecret))
                throw new ArgumentException("GithubAuthenticationProvider: please specify clientId and clientSecret in the configuration!");
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
            if (code == null)
                throw new AuthenticationException("OAuth authentication code was not provided in the request object!");

            var client = new HttpClient { BaseAddress = new Uri("https://api.github.com") };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("DeviceHive")));

            // exchange auth code to the access token
            var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", ProviderConfiguration.ClientId },
                { "client_secret", ProviderConfiguration.ClientSecret },
                { "redirect_uri", DeviceHiveConfiguration.Authentication.OAuthRedirectUri },
                { "code", code },
            });

            var tokenResponse = await client.PostAsync("https://github.com/login/oauth/access_token", tokenRequest);
            if (!tokenResponse.IsSuccessStatusCode)
                throw new AuthenticationException("Invalid access token!");

            var tokenResponseJson = JObject.Parse(await tokenResponse.Content.ReadAsStringAsync());
            var token = (string)tokenResponseJson["access_token"];

            // get user GitHub primary email
            var userEmailsResponse = await client.GetAsync("user/emails?access_token=" + token);
            if (!userEmailsResponse.IsSuccessStatusCode)
                throw new AuthenticationException("Invalid access token!");

            var userEmailsResponseJson = JArray.Parse(await userEmailsResponse.Content.ReadAsStringAsync());
            var userPrimaryEmail = userEmailsResponseJson.FirstOrDefault(e => (bool)e["primary"]);
            if (userPrimaryEmail == null)
                throw new AuthenticationException("GitHub did not provide user email associated with the token!");

            var login = (string)userPrimaryEmail["email"];

            // get user from database
            var user = DataContext.User.GetByGithubLogin(login);
            if (user == null || user.Status != (int)UserStatus.Active)
                throw new AuthenticationException("Unknown GitHub login, or user is not active!");

            UpdateUserLastLogin(user);
            return user; // success
        }
        #endregion
    }
}
