using DeviceHive.API.Filters;
using DeviceHive.Core.Authentication;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;

namespace DeviceHive.API.Controllers
{
    [RoutePrefix("oauth2")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class OAuth2AccessKeyController : BaseController
    {
        private IAuthenticationManager _authenticationManager;

        public OAuth2AccessKeyController(IAuthenticationManager authenticationManager)
        {
            _authenticationManager = authenticationManager;
        }

        [HttpPost, Route("accesskey")]
        public async Task<JObject> Create(FormDataCollection formDataRequest)
        {
            var request = new JObject(formDataRequest.Select(r => new JProperty(r.Key, r.Value)));

            var query = HttpUtility.ParseQueryString((string)request["state"]);
            var providerId = int.Parse(query["identity_provider_id"]);

            var providers = _authenticationManager.GetProviders();
            var providerName = providers[providerId].Name;

            User user = null;
            try
            {
                user = await _authenticationManager.AuthenticateAsync(providerName, request);
            }
            catch (AuthenticationException)
            {
                ThrowHttpResponse(HttpStatusCode.Unauthorized, "Not authorized!");
            }

            var accessKey = RenewAccessKey(user);
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

        private AccessKey RenewAccessKey(User user)
        {
            const string Label = "OAuth token";

            var accessKeys = DataContext.AccessKey.GetByUser(user.ID);
            var accessKey = accessKeys.FirstOrDefault(a => a.Label == Label);
            if (accessKey == null)
            {
                accessKey = new AccessKey(user.ID, Label);
                accessKey.Permissions = new List<AccessKeyPermission>();
                accessKey.Permissions.Add(new AccessKeyPermission()); // allow everything permission
            }

            accessKey.GenerateKey(); // regenerate key
            accessKey.ExpirationDate = DateTime.UtcNow.Add(DeviceHiveConfiguration.Authentication.SessionTimeout);
            
            DataContext.AccessKey.Save(accessKey);
            return accessKey;
        }
    }
}