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
        #region Public Properties

        public bool ThrowDeviceNotFoundException { get; set; }

        #endregion

        #region Constructor

        public AuthenticateDeviceAttribute()
            : this(true)
        {
        }

        public AuthenticateDeviceAttribute(bool throwDeviceNotFoundException)
        {
            ThrowDeviceNotFoundException = throwDeviceNotFoundException;
        }
        #endregion

        #region ActionFilterAttribute Members

        public override void OnAuthentication(ActionContext actionContext)
        {
            var request = actionContext.Request;
            if (request == null)
                return;

            var deviceId = (string)request["deviceId"];
            var deviceKey = (string)request["deviceKey"];
            if (deviceId == null || deviceKey == null)
                return;

            var controller = (DeviceHive.WebSockets.API.Controllers.ControllerBase)actionContext.Controller;
            var device = controller.DataContext.Device.Get(deviceId);
            if (device == null || device.Key == null || device.Key != deviceKey)
            {
                if (ThrowDeviceNotFoundException)
                    throw new WebSocketRequestException("Device not found");
                return;
            }

            controller.DataContext.Device.SetLastOnline(device.ID);
            actionContext.Parameters["AuthDevice"] = device;
        }

        #endregion
    }
}
