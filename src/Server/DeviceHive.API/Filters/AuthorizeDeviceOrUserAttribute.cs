using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace DeviceHive.API.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class AuthorizeDeviceOrUserAttribute : AuthorizationFilterAttribute
    {
        public string Roles { get; set; }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            // try to authorize as device
            try
            {
                var authorizeDeviceAttribute = new AuthorizeDeviceAttribute();
                authorizeDeviceAttribute.OnAuthorization(actionContext);
                return;
            }
            catch (HttpResponseException)
            {
            }

            // try to authorize as user
            var authorizeUserAttribute = new AuthorizeUserAttribute { Roles = Roles };
            authorizeUserAttribute.OnAuthorization(actionContext);
            return;
        }
    }
}