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
    /// Represents enumeration of available entities for authorization.
    /// </summary>
    [Flags]
    public enum AuthorizeEntity
    {
        /// <summary>
        /// User entity.
        /// </summary>
        User = 1,

        /// <summary>
        /// Device entity.
        /// </summary>
        Device = 2,
    }

    /// <summary>
    /// Requires entity authorization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class AuthorizeAttribute : AuthorizationFilterAttribute
    {
        #region Public Properties

        /// <summary>
        /// Specifies enumeration of allowed authenticated entities.
        /// </summary>
        public AuthorizeEntity Entity { get; private set; }

        /// <summary>
        /// For user entities, specifies a comma-separated list of allowed user roles.
        /// If set to null, all user roles are allowed.
        /// </summary>
        public string Roles { get; set; }

        /// <summary>
        /// When using access key authentication, specifies required access key action.
        /// If set to null, access keys authentication is not allowed.
        /// </summary>
        public string AccessKeyAction { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="entity">AuthorizeEntity enumeration which represents allowed entities.</param>
        public AuthorizeAttribute(AuthorizeEntity entity)
        {
            Entity = entity;
        }
        #endregion

        #region AuthorizationFilterAttribute Members

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var controller = actionContext.ControllerContext.Controller as BaseController;
            if (controller == null)
                throw new InvalidOperationException("Controller must inherit from BaseController class!");

            if (actionContext.Request.Method == HttpMethod.Options)
                return; // do not require authorization for CORS preflight requests

            if ((Entity & AuthorizeEntity.Device) != 0)
            {
                if (TryAuthorizeDevice(actionContext, controller.CallContext))
                    return; // device authorization is successful
            }

            if ((Entity & AuthorizeEntity.User) != 0)
            {
                if (TryAuthorizeUser(actionContext, controller.CallContext))
                    return; // user authorization is successful
            }

            // authorization is not successful
            ThrowUnauthorizedResponse(actionContext);
        }
        #endregion

        #region Protected Methods

        protected virtual bool TryAuthorizeDevice(HttpActionContext actionContext, CallContext callContext)
        {
            return callContext.CurrentDevice != null;
        }

        protected virtual bool TryAuthorizeUser(HttpActionContext actionContext, CallContext callContext)
        {
            // check if user is authenticated
            if (callContext.CurrentUser == null)
                return false;

            // allow access key authentication only if AccessKeyAction is specified
            if (callContext.CurrentAccessKey != null && AccessKeyAction == null)
                return false;

            // check if user role is allowed
            if (Roles != null)
            {
                var currentUserRole = ((UserRole)callContext.CurrentUser.Role).ToString();
                if (!Roles.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Contains(currentUserRole))
                    return false;
            }

            // check if access key permissions are sufficient
            if (callContext.CurrentAccessKey != null)
            {
                var httpContext = (HttpContextBase)actionContext.Request.Properties["MS_HttpContext"];
                var userAddress = httpContext.Request.UserHostAddress;

                var domain = actionContext.Request.Headers.Contains("Origin") ?
                    actionContext.Request.Headers.GetValues("Origin").First() : null;
                if (domain != null)
                    domain = Regex.Replace(domain, @"^https?://", string.Empty, RegexOptions.IgnoreCase);

                var permissions = callContext.CurrentAccessKey.Permissions
                    .Where(p => p.IsActionAllowed(AccessKeyAction) &&
                        p.IsAddressAllowed(userAddress) && (domain == null || p.IsDomainAllowed(domain)))
                    .ToArray();

                if (!permissions.Any())
                    return false;

                callContext.CurrentUserPermissions = permissions;
            }

            // authorization is successful
            return true;
        }

        protected virtual void ThrowUnauthorizedResponse(HttpActionContext actionContext)
        {
            var response = actionContext.Request.CreateResponse<ErrorDetail>(HttpStatusCode.Unauthorized, new ErrorDetail("Not authorized"));

            if ((Entity & AuthorizeEntity.User) != 0)
            {
                AllowCrossDomainOrigin.AppendCorsHeaders(actionContext.Request, response);
            }

            throw new HttpResponseException(response);
        }
        #endregion
    }

    /// <summary>
    /// Requires user authorization with Administrator role.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class AuthorizeAdminAttribute : AuthorizeAttribute
    {
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AuthorizeAdminAttribute()
            : base(AuthorizeEntity.User)
        {
            Roles = "Administrator";
        }
        #endregion
    }

    /// <summary>
    /// Requires user authorization with any role.
    /// Specify AccessKeyAction property to authorize access keys with the corresponding permission.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class AuthorizeUserAttribute : AuthorizeAttribute
    {
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AuthorizeUserAttribute()
            : base(AuthorizeEntity.User)
        {
        }
        #endregion
    }

    /// <summary>
    /// Requires user or device authorization.
    /// Specify AccessKeyAction property to authorize access keys with the corresponding permission.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class AuthorizeUserOrDeviceAttribute : AuthorizeAttribute
    {
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AuthorizeUserOrDeviceAttribute()
            : base(AuthorizeEntity.User | AuthorizeEntity.Device)
        {
        }
        #endregion
    }
}