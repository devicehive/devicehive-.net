using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.WebSockets.Core.ActionsFramework;
using DeviceHive.WebSockets.Core.Network;
using DeviceHive.Data.Model;

namespace DeviceHive.WebSockets.API.Filters
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AuthorizeDeviceAttribute : ActionFilterAttribute
    {
        #region ActionFilterAttribute Members

        public override void OnAuthorization(ActionContext actionContext)
        {
            var device = (Device)(actionContext.GetParameter("AuthDevice") ?? actionContext.Connection.Session["Device"]);
            if (device == null)
                throw new WebSocketRequestException("Please authenticate to invoke this action");
        }

        #endregion
    }
}
