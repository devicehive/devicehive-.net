using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using DeviceHive.API.Controllers;
using DeviceHive.API.Models;

namespace DeviceHive.API.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class AuthorizeDeviceAttribute : AuthorizationFilterAttribute
    {
        protected DataContext DataContext { get; private set; }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            // initialize current filter
            var controller = (BaseController)actionContext.ControllerContext.Controller;
            DataContext = controller.DataContext;

            // check if device is authenticated
            if (controller.RequestContext.CurrentDevice == null)
                ThrowUnauthorizedResponse(actionContext);
        }

        private void ThrowUnauthorizedResponse(HttpActionContext actionContext)
        {
            var response = actionContext.Request.CreateResponse<ErrorDetail>(HttpStatusCode.Unauthorized, new ErrorDetail("Not authorized"));
            throw new HttpResponseException(response);
        }
    }
}