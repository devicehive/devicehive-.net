using DeviceHive.API.Filters;
using DeviceHive.Core.Authentication;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace DeviceHive.API.Controllers
{
    /// <resource name="Authentication">
    /// Represents utility methods for user authentication and generating session-level access tokens.
    /// </resource>
    [RoutePrefix("auth")]
    public class AuthController : BaseController
    {
        private IAuthenticationManager _authenticationManager;

        public AuthController(IAuthenticationManager authenticationManager)
        {
            _authenticationManager = authenticationManager;
        }

        /// <name>config</name>
        /// <summary>
        /// Gets information about supported authentication providers.
        /// </summary>
        /// <returns>If successful, this method returns the following resource in the response body.</returns>
        /// <response>
        ///     <parameter name="providers" type="array">List of authentication providers supported by this instance.</parameter>
        ///     <parameter name="providers[].name" type="string">Provider name.</parameter>
        ///     <parameter name="providers[].clientId" type="string">Client identifier of DeviceHive application within the third-party provider.</parameter>
        ///     <parameter name="sessionTimeout" type="integer">User session timeout in seconds.</parameter>
        /// </response>
        [HttpGet, Route("~/info/config/auth")]
        public JObject Config()
        {
            return new JObject(
                new JProperty("providers", _authenticationManager.GetProviders().Select(p => new JObject(
                    new JProperty("name", p.Name),
                    new JProperty("clientId", p.Configuration.ClientId)
                ))),
                new JProperty("sessionTimeout", (int)DeviceHiveConfiguration.Authentication.SessionTimeout.TotalSeconds));
        }

        /// <name>login</name>
        /// <summary>
        /// Authenticates a user and returns a session-level access key.
        /// </summary>
        /// <param name="request">In the request body, supply an object with the following properties.</param>
        /// <returns>If successful, this method returns the object with the following properties in the response body.</returns>
        /// <request>
        ///     <parameter name="providerName" type="string" required="true">Name of authentication provider to use. Please call the 'config' method to get the list of available authentication providers. Use the 'password' value for the password-based authentication.</parameter>
        ///     <parameter name="login" type="string">When using password authentication, specifies user login.</parameter>
        ///     <parameter name="password" type="string">When using password authentication, specifies user password.</parameter>
        ///     <parameter name="code" type="string">When using OAuth authentication, specifies authorization code received from provider.</parameter>
        ///     <parameter name="redirect_uri" type="string">When using OAuth authentication, specifies redirect uri used during authentication.</parameter>
        ///     <parameter name="access_token" type="string">When using OAuth implicit authentication, specifies access code issued to the DeviceHive application.</parameter>
        /// </request>
        /// <response>
        ///     <parameter name="key" type="string">Session-level access key to use with this API.</parameter>
        /// </response>
        [HttpPost, Route("accesskey")]
        public async Task<JObject> Create(JObject request)
        {
            var providerName = (string)request["providerName"];
            if (string.IsNullOrEmpty(providerName))
                ThrowHttpResponse(HttpStatusCode.BadRequest, "providerName is required!");

            User user = null;
            try
            {
                user = await _authenticationManager.AuthenticateAsync(providerName, request);
            }
            catch (AuthenticationException)
            {
                ThrowHttpResponse(HttpStatusCode.Unauthorized, "Not authorized!");
            }

            var accessKey = CreateAccessKey(user);
            return new JObject(new JProperty("key", accessKey.Key));
        }

        /// <name>logout</name>
        /// <summary>
        /// Invalidates the session-level token.
        /// </summary>
        [HttpNoContentResponse]
        [HttpDelete, Route("accesskey")]
        [AuthorizeUser(AccessKeyAction = "GetCurrentUser")]
        public void Delete()
        {
            if (CallContext.CurrentAccessKey != null)
                DataContext.AccessKey.Delete(CallContext.CurrentAccessKey.ID);
        }

        private AccessKey CreateAccessKey(User user)
        {
            const string Label = "Session Token";

            var accessKey = new AccessKey(user.ID, AccessKeyType.Session, Label);
            accessKey.Permissions = new List<AccessKeyPermission>();
            accessKey.Permissions.Add(new AccessKeyPermission()); // allow everything permission
            accessKey.ExpirationDate = DateTime.UtcNow.Add(DeviceHiveConfiguration.Authentication.SessionTimeout);

            DataContext.AccessKey.Save(accessKey);
            return accessKey;
        }
    }
}