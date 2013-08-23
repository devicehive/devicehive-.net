using System;
using System.Linq;
using DeviceHive.WebSockets.Core.ActionsFramework;
using DeviceHive.WebSockets.Core.Network;
using DeviceHive.Data.Model;

namespace DeviceHive.WebSockets.API.Filters
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AuthorizeClientAttribute : ActionFilterAttribute
    {
        #region Public Properties

        public string AccessKeyAction { get; set; }

        #endregion

        #region ActionFilterAttribute Members

        public override void OnAuthorization(ActionContext actionContext)
        {
            var connection = actionContext.Connection;
            var user = (User)connection.Session["User"];
            var accessKey = (AccessKey)connection.Session["AccessKey"];
            if (user == null)
                throw new WebSocketRequestException("Please authenticate to invoke this action");

            // check if access key permissions are sufficient
            if (accessKey != null && (AccessKeyAction == null ||
                !accessKey.Permissions.Any(p => p.IsActionAllowed(AccessKeyAction) && p.IsAddressAllowed(connection.Host))))
            {
                throw new WebSocketRequestException("The access key used does not allow execution of this action");
            }
        }

        #endregion
    }
}
