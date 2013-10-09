using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.Description;
using DeviceHive.API.Models;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;

namespace DeviceHive.API.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class OAuth2Controller : BaseController
    {
        [HttpPost]
        public JObject Token(FormDataCollection request)
        {
            var clientId = request.Get("client_id");
            if (string.IsNullOrEmpty(clientId))
                ThrowHttpResponse(HttpStatusCode.BadRequest, "Missing client_id parameter!");

            var clientSecret = request.Get("client_secret");
            if (string.IsNullOrEmpty(clientSecret))
                ThrowHttpResponse(HttpStatusCode.BadRequest, "Missing client_secret parameter!");

            var code = request.Get("code");
            if (string.IsNullOrEmpty(code))
                ThrowHttpResponse(HttpStatusCode.BadRequest, "Missing code parameter!");

            var redirectUri = request.Get("redirect_uri");
            if (string.IsNullOrEmpty(redirectUri))
                ThrowHttpResponse(HttpStatusCode.BadRequest, "Missing redirect_uri parameter!");

            var grantType = request.Get("grant_type");
            if (string.IsNullOrEmpty(grantType))
                ThrowHttpResponse(HttpStatusCode.BadRequest, "Missing grant_type parameter!");
            if (grantType != "authorization_code")
                ThrowHttpResponse(HttpStatusCode.BadRequest, "Invalid grant_type parameter!");

            var client = DataContext.OAuthClient.Get(clientId);
            if (client == null || client.OAuthSecret != clientSecret)
                ThrowHttpResponse(HttpStatusCode.Unauthorized, "Not authorized!");

            Guid authCode;
            if (!Guid.TryParse(code, out authCode))
                ThrowHttpResponse(HttpStatusCode.Forbidden, "Invalid authorization code!");

            var grant = DataContext.OAuthGrant.Get(authCode);
            if (grant == null || grant.ClientID != client.ID || grant.Type != (int)OAuthGrantType.Code || grant.RedirectUri != redirectUri)
                ThrowHttpResponse(HttpStatusCode.Forbidden, "Invalid authorization code!");

            var accessKey = grant.AccessKey;
            if (accessKey.ExpirationDate != null && accessKey.ExpirationDate.Value < DataContext.Timestamp.GetCurrentTimestamp())
                ThrowHttpResponse(HttpStatusCode.Forbidden, "Invalid authorization code!");

            return new JObject(
                new JProperty("access_token", accessKey.Key));
        }
    }
}
