using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using DeviceHive.API.Controllers;
using DeviceHive.API.Models;
using DeviceHive.Data.Model;

namespace DeviceHive.API.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class AuthorizeUserAttribute : AuthorizationFilterAttribute
    {
        public string Roles { get; set; }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            // initialize current filter
            var controller = (BaseController)actionContext.ControllerContext.Controller;

            // check if user is authenticated
            if (controller.RequestContext.CurrentUser == null)
                ThrowUnauthorizedResponse(actionContext);

            // check user role
            var currentUserRole = ((UserRole)controller.RequestContext.CurrentUser.Role).ToString();
            if (Roles != null && !Roles.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Contains(currentUserRole))
                ThrowUnauthorizedResponse(actionContext);
        }

        private void ThrowUnauthorizedResponse(HttpActionContext actionContext)
        {
            var response = actionContext.Request.CreateResponse<ErrorDetail>(HttpStatusCode.Unauthorized, new ErrorDetail("Not authorized"));
            response.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue("Basic"));

            var origin = actionContext.Request.Headers.FirstOrDefault(h => h.Key == "Origin");
            if (origin.Value != null && origin.Value.Any())
            {
                response.Headers.Add("Access-Control-Allow-Origin", origin.Value.First());
                response.Headers.Add("Access-Control-Allow-Credentials", "true");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
                response.Headers.Add("Access-Control-Allow-Headers", "Origin, Authorization, Accept");
            }

            throw new HttpResponseException(response);
        }
    }
}