using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Data.Model;
using DeviceHive.WebSockets.Core.ActionsFramework;

namespace DeviceHive.WebSockets.API.Filters
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AuthorizeDeviceRegistrationAttribute : ActionFilterAttribute
    {
        public override void OnAuthorization(ActionContext actionContext)
        {
            var controller = (DeviceHive.WebSockets.API.Controllers.ControllerBase)actionContext.Controller;

            var deviceId = (Guid)actionContext.Request["deviceId"];
            var device = controller.DataContext.Device.Get(deviceId);
            if (device != null)
            {
                var authDevice = (Device)(actionContext.GetParameter("AuthDevice") ?? actionContext.Connection.Session["Device"]);
                if (authDevice == null || authDevice.ID != device.ID)
                    throw new WebSocketRequestException("Not authorized");

                actionContext.Parameters["Device"] = device;
            }
        }
    }
}
