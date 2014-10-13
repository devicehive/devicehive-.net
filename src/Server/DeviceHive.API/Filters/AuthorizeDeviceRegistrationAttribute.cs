using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Controllers;
using DeviceHive.API.Controllers;

namespace DeviceHive.API.Filters
{
    /// <summary>
    /// Requires user or device authorization in the case when an existing device identifier is passed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class AuthorizeDeviceRegistrationAttribute : AuthorizeUserOrDeviceAttribute
    {
        public string DeviceIdParamName { get; private set; }

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="deviceIdParamName">Name of the device identifier parameter.</param>
        public AuthorizeDeviceRegistrationAttribute(string deviceIdParamName = "id")
        {
            if (string.IsNullOrEmpty(deviceIdParamName))
                throw new ArgumentException("deviceIdParamName is null or empty!", "deviceIdParamName");

            DeviceIdParamName = deviceIdParamName;
        }
        #endregion

        #region AuthorizationFilterAttribute Members

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var controllerContext = actionContext.ControllerContext;
            var controller = actionContext.ControllerContext.Controller as BaseController;
            if (controller == null)
                throw new InvalidOperationException("Controller must inherit from BaseController class!");

            var deviceId = (string)controllerContext.RouteData.Values[DeviceIdParamName];
            var device = controller.DataContext.Device.Get(deviceId);
            if (device != null)
            {
                base.OnAuthorization(actionContext);
                actionContext.Request.Properties["Device"] = device;
            }
        }
        #endregion
    }
}