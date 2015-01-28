using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.Description;
using DeviceHive.API.Models;
using DeviceHive.Core.Authentication;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;
using System.Text;

namespace DeviceHive.API.Controllers
{
    [RoutePrefix("oauth2")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class OAuth2Controller : BaseController
    {
        private IAuthenticationManager _authenticationManager;

        public OAuth2Controller(IAuthenticationManager authenticationManager)
        {
            _authenticationManager = authenticationManager;
        }

        [HttpPost, Route("token")]
        public JObject Token(FormDataCollection request)
        {
            var client = AuthenticateClient(Request, request);

            AccessKey accessKey = null;
            var grantType = GetRequiredParameter(request, "grant_type");
            switch (grantType)
            {
                case "authorization_code":
                    {
                        var code = GetRequiredParameter(request, "code");
                        var redirectUri = GetRequiredParameter(request, "redirect_uri");

                        Guid authCode;
                        if (!Guid.TryParse(code, out authCode))
                            ThrowHttpResponse(HttpStatusCode.Forbidden, "Invalid authorization code!");

                        // find a valid grant by authorization code
                        var grant = DataContext.OAuthGrant.Get(authCode);
                        if (grant == null || grant.ClientID != client.ID || grant.Type != (int)OAuthGrantType.Code || grant.RedirectUri != redirectUri)
                            ThrowHttpResponse(HttpStatusCode.Forbidden, "Invalid authorization code!");

                        if (DateTime.UtcNow > grant.Timestamp.AddMinutes(10))
                            ThrowHttpResponse(HttpStatusCode.Forbidden, "Invalid authorization code!");

                        grant.AuthCode = null; // deny subsequent requests with the same authorization code
                        DataContext.OAuthGrant.Save(grant);

                        accessKey = grant.AccessKey;
                    }
                    break;
                
                case "password":
                    {
                        var scope = GetRequiredParameter(request, "scope");
                        var username = GetRequiredParameter(request, "username");
                        var password = GetRequiredParameter(request, "password");

                        // authenticate user
                        User user = null;
                        try
                        {
                            user = _authenticationManager.AuthenticateByPassword(username, password);
                        }
                        catch (AuthenticationException)
                        {
                            ThrowHttpResponse(HttpStatusCode.Unauthorized, "Invalid credentials or account is disabled!");
                        }

                        // issue or renew grant
                        var filter = new OAuthGrantFilter
                            {
                                ClientID = client.ID,
                                Scope = scope,
                                Type = (int)OAuthGrantType.Password,
                            };

                        var grant = DataContext.OAuthGrant.GetByUser(user.ID, filter).FirstOrDefault() ??
                            new OAuthGrant(client, user.ID, new AccessKey(), (int)OAuthGrantType.Password, scope);
                        RenewGrant(grant);

                        DataContext.AccessKey.Save(grant.AccessKey);
                        DataContext.OAuthGrant.Save(grant);
                        accessKey = grant.AccessKey;
                    }
                    break;
                
                default:
                    ThrowHttpResponse(HttpStatusCode.BadRequest, "Invalid grant_type parameter!");
                    break;
            }

            return new JObject(
                new JProperty("access_token", accessKey.Key),
                new JProperty("token_type", "Bearer"),
                new JProperty("expires_in", accessKey.ExpirationDate == null ? null :
                    (int?)(int)accessKey.ExpirationDate.Value.Subtract(DateTime.UtcNow).TotalSeconds));
        }

        private OAuthClient AuthenticateClient(HttpRequestMessage request, FormDataCollection form)
        {
            string clientId = null;
            string clientSecret = null;

            // try to get credentials from Authorization header
            var auth = request.Headers.Authorization;
            if (auth != null && auth.Scheme == "Basic" && !string.IsNullOrEmpty(auth.Parameter))
            {
                try
                {
                    var authParam = Encoding.UTF8.GetString(Convert.FromBase64String(auth.Parameter));
                    if (!authParam.Contains(":"))
                        throw new FormatException();

                    clientId = authParam.Substring(0, authParam.IndexOf(':'));
                    clientSecret = authParam.Substring(authParam.IndexOf(':') + 1);
                }
                catch (FormatException)
                {
                }
            }

            // try to get credentials from form parameters
            if (clientId == null)
            {
                clientId = GetRequiredParameter(form, "client_id");
                clientSecret = GetRequiredParameter(form, "client_secret");
            }

            // authenticate client
            var client = DataContext.OAuthClient.Get(clientId);
            if (client == null || client.OAuthSecret != clientSecret)
                ThrowHttpResponse(HttpStatusCode.Unauthorized, "Not authorized!");

            return client;
        }

        private string GetRequiredParameter(FormDataCollection request, string parameter)
        {
            var value = request.Get(parameter);
            if (string.IsNullOrEmpty(value))
                ThrowHttpResponse(HttpStatusCode.BadRequest, "Missing required parameter: " + parameter);

            return value;
        }

        internal static void RenewGrant(OAuthGrant grant)
        {
            grant.AccessKey = grant.AccessKey ?? new AccessKey();
            grant.AccessKey.GenerateKey();
            grant.AccessKey.UserID = grant.UserID;
            grant.AccessKey.Label = "OAuth token for: " + grant.Client.Name;
            grant.AccessKey.ExpirationDate = grant.AccessType == (int)OAuthGrantAccessType.Online ? (DateTime?)DateTime.UtcNow.AddHours(1) : null;

            grant.AccessKey.Permissions = new List<AccessKeyPermission>();
            grant.AccessKey.Permissions.Add(new AccessKeyPermission
                {
                    Subnets = grant.Client.Subnet == null ? null : grant.Client.Subnet.Split(','),
                    Domains = new[] { grant.Client.Domain },
                    Actions = grant.Scope.Split(' '),
                    Networks = grant.Networks,
                });

            grant.Timestamp = DateTime.UtcNow;
            grant.AuthCode = grant.Type == (int)OAuthGrantType.Code ? (Guid?)Guid.NewGuid() : null;
        }
    }
}
