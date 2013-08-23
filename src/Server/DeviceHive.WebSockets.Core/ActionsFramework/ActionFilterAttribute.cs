using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.WebSockets.Core.Network;

namespace DeviceHive.WebSockets.Core.ActionsFramework
{
    public abstract class ActionFilterAttribute : Attribute
    {
        public virtual void OnAuthentication(ActionContext actionContext)
        {
        }

        public virtual void OnAuthorization(ActionContext actionContext)
        {
        }
    }
}
