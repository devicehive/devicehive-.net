using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using DeviceHive.API.Controllers;
using DeviceHive.API.Models;
using DeviceHive.Data.Model;
using System.Web;

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

            if ((Entity & AuthorizeEntity.Device) != 0)
            {
                if (TryAuthorizeDevice(actionContext, controller.RequestContext))
                    return; // device authorization is successful
            }

            if ((Entity & AuthorizeEntity.User) != 0)
            {
                if (TryAuthorizeUser(actionContext, controller.RequestContext))
                    return; // user authorization is successful
            }

            // authorization is not successful
            ThrowUnauthorizedResponse(actionContext);
        }
        #endregion

        #region Protected Methods

        protected virtual bool TryAuthorizeDevice(HttpActionContext actionContext, RequestContext requestContext)
        {
            return requestContext.CurrentDevice != null;
        }

        protected virtual bool TryAuthorizeUser(HttpActionContext actionContext, RequestContext requestContext)
        {
            // check if user is authenticated
            if (requestContext.CurrentUser == null)
                return false;

            // allow access key authentication only if AccessKeyAction is specified
            if (requestContext.CurrentAccessKey != null && AccessKeyAction == null)
                return false;

            // check if user role is allowed
            if (Roles != null)
            {
                var currentUserRole = ((UserRole)requestContext.CurrentUser.Role).ToString();
                if (!Roles.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Contains(currentUserRole))
                    return false;
            }

            // check if access key permissions are sufficient
            if (requestContext.CurrentAccessKey != null)
            {
                var httpContext = (HttpContextBase)actionContext.Request.Properties["MS_HttpContext"];
                var userAddress = httpContext.Request.UserHostAddress;

                var permissions = requestContext.CurrentAccessKey.Permissions
                    .Where(p => p.IsActionAllowed(AccessKeyAction) && p.IsAddressAllowed(userAddress)) // TODO: domains
                    .ToArray();

                if (!permissions.Any())
                    return false;

                requestContext.CurrentUserPermissions = permissions;
            }

            // authorization is successful
            return true;
        }

        protected virtual void ThrowUnauthorizedResponse(HttpActionContext actionContext)
        {
            var response = actionContext.Request.CreateResponse<ErrorDetail>(HttpStatusCode.Unauthorized, new ErrorDetail("Not authorized"));

            if ((Entity & AuthorizeEntity.User) != 0)
            {
                response.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue("Basic"));

                var origin = actionContext.Request.Headers.FirstOrDefault(h => h.Key == "Origin");
                if (origin.Value != null && origin.Value.Any())
                {
                    response.Headers.Add("Access-Control-Allow-Origin", origin.Value.First());
                    response.Headers.Add("Access-Control-Allow-Credentials", "true");
                    response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
                    response.Headers.Add("Access-Control-Allow-Headers", "Origin, Authorization, Accept, Content-Type");
                }
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