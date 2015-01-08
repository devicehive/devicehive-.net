using DeviceHive.API.Filters;
using DeviceHive.Core.Authentication;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace DeviceHive.API.Controllers
{
    [RoutePrefix("auth")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AuthController : BaseController
    {
        private IAuthenticationManager _authenticationManager;

        public AuthController(IAuthenticationManager authenticationManager)
        {
            _authenticationManager = authenticationManager;
        }

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