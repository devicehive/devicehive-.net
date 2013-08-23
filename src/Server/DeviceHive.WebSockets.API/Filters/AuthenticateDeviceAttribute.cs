using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.WebSockets.Core.ActionsFramework;
using DeviceHive.WebSockets.Core.Network;

namespace DeviceHive.WebSockets.API.Filters
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AuthenticateDeviceAttribute : ActionFilterAttribute
    {
        #region ActionFilterAttribute Members

        public override void OnAuthentication(ActionContext actionContext)
        {
            var request = actionContext.Request;
            if (request == null)
                return;

            var deviceIdValue = request["deviceId"];
            var deviceKeyValue = request["deviceKey"];
            if (deviceIdValue == null || deviceKeyValue == null)
                return;

            var deviceId = Guid.Parse((string)deviceIdValue);
            var deviceKey = (string)deviceKeyValue;

            var controller = (DeviceHive.WebSockets.API.Controllers.ControllerBase)actionContext.Controller;
            var device = controller.DataContext.Device.Get(deviceId);
            if (device == null || device.Key != deviceKey)
                throw new WebSocketRequestException("Device not found");

            actionContext.Parameters["AuthDevice"] = device;
        }

        #endregion
    }
}
