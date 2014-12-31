using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using DeviceHive.API.Controllers;
using DeviceHive.API.Models;
using DeviceHive.Data.Model;

namespace DeviceHive.API.Filters
{
    /// <summary>
    /// Requires user authorization with Administrator role, or user authorization with any role if the UserIdParamName parameter value matches current user ID.
    /// Specify AccessKeyAction property to authorize access keys with the corresponding permission.
    /// Specify UserAccessKeyAction property to authorize access keys when the UserIdParamName parameter matches current user ID.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class AuthorizeAdminOrCurrentUser : AuthorizeAdminAttribute
    {
        private const string CURRENT_VALUE = "current";

        #region Public Properties

        /// <summary>
        /// Gets the name of parameter which needs to be compared with current user ID to decide whether the Administrator role is required.
        /// </summary>
        public string UserIdParamName { get; private set; }

        /// <summary>
        /// When using access key authentication, specifies required access key action when the UserIdParamName parameter value matches current user ID.
        /// If set to null, access keys authentication is not allowed in such case.
        /// </summary>
        public string CurrentUserAccessKeyAction { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="userIdParamName">Name of parameter which needs to be compared with current user ID to decide whether the Administrator role is required.</param>
        public AuthorizeAdminOrCurrentUser(string userIdParamName = "userId")
        {
            if (string.IsNullOrEmpty(userIdParamName))
                throw new ArgumentException("userIdParamName is null or empty!", "userIdParamName");

            UserIdParamName = userIdParamName;
        }
        #endregion

        #region AuthorizationFilterAttribute Members

        protected override bool TryAuthorizeUser(HttpActionContext actionContext, CallContext callContext, string roles, string accessKeyAction)
        {
            var controllerContext = actionContext.ControllerContext;

            if (callContext.CurrentUser != null)
            {
                // replace 'current' keyword with the current user ID
                var userId = controllerContext.RouteData.Values[UserIdParamName].ToString();
                if (string.Equals(userId, CURRENT_VALUE, StringComparison.OrdinalIgnoreCase))
                {
                    controllerContext.RouteData.Values[UserIdParamName] = callContext.CurrentUser.ID.ToString();
                    userId = callContext.CurrentUser.ID.ToString();
                }

                // if user ID matches current user ID, perform authorization for any role
                int intUserId;
                if (int.TryParse(userId, out intUserId) && intUserId == callContext.CurrentUser.ID)
                    return base.TryAuthorizeUser(actionContext, callContext, null, CurrentUserAccessKeyAction);
            }

            // otherwise run admin authorization routine
            return base.TryAuthorizeUser(actionContext, callContext, roles, accessKeyAction);
        }
        #endregion
    }
}