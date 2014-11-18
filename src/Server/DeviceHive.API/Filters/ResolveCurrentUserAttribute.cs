using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using DeviceHive.API.Controllers;

namespace DeviceHive.API.Filters
{
    public class ResolveCurrentUserAttribute : ActionFilterAttribute
    {
        private const string CURRENT_VALUE = "current";

        public string UserIdParamName { get; private set; }

        public ResolveCurrentUserAttribute(string userIdParamName = "userId")
        {
            if (string.IsNullOrEmpty(userIdParamName))
                throw new ArgumentException("userIdParamName is null or empty!", "userIdParamName");

            UserIdParamName = userIdParamName;
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            base.OnActionExecuting(actionContext);

            var controllerContext = actionContext.ControllerContext;
            if (string.Equals((string)controllerContext.RouteData.Values[UserIdParamName], CURRENT_VALUE, StringComparison.OrdinalIgnoreCase))
            {
                var controller = (BaseController)controllerContext.Controller;
                if (controller.CallContext.CurrentUser != null)
                    actionContext.ActionArguments[UserIdParamName] = controller.CallContext.CurrentUser.ID;
            }
        }
    }
}
