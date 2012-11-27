using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using DeviceHive.API.Controllers;
using DeviceHive.Data.Model;

namespace DeviceHive.API.Filters
{
    public class AuthenticateAttribute : AuthorizationFilterAttribute
    {
        protected DataContext DataContext { get; private set; }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            // initialize current filter
            var controller = (BaseController)actionContext.ControllerContext.Controller;
            DataContext = controller.DataContext;

            // check basic authorization
            var auth = actionContext.Request.Headers.Authorization;
            if (auth != null && auth.Scheme == "Basic" && !string.IsNullOrEmpty(auth.Parameter))
            {
                // parse the authorization header
                string login, password;
                try
                {
                    var authParam = Encoding.UTF8.GetString(Convert.FromBase64String(auth.Parameter));
                    if (!authParam.Contains(":"))
                        throw new FormatException();

                    login = authParam.Substring(0, authParam.IndexOf(':'));
                    password = authParam.Substring(authParam.IndexOf(':') + 1);
                }
                catch (FormatException)
                {
                    return;
                }

                // get the user object
                var user = DataContext.User.Get(login);
                if (user != null && user.Status == (int)UserStatus.Active)
                {
                    // check user password
                    if (user.IsValidPassword(password))
                    {
                        // authenticate the user
                        UpdateUserLastLogin(user);
                        controller.RequestContext.CurrentUser = user;
                    }
                    else
                    {
                        IncrementUserLoginAttempts(user);
                    }
                }
                
                return;
            }

            // check device authentication
            Guid deviceId;
            var authDeviceId = GetCustomHeader(actionContext, "Auth-DeviceID");
            if (!string.IsNullOrEmpty(authDeviceId) && Guid.TryParse(authDeviceId, out deviceId))
            {
                // get the device object
                var device = DataContext.Device.Get(deviceId);
                if (device != null)
                {
                    // check device key authentication
                    var authDeviceKey = GetCustomHeader(actionContext, "Auth-DeviceKey");
                    if (authDeviceKey != null && device.Key == authDeviceKey)
                    {
                        // authenticate the device
                        controller.RequestContext.CurrentDevice = device;
                    }
                }

                return;
            }
        }

        private void IncrementUserLoginAttempts(User user)
        {
            user.LoginAttempts++;
            if (user.LoginAttempts >= 10)
                user.Status = (int)UserStatus.LockedOut;
            DataContext.User.Save(user);
        }

        private void UpdateUserLastLogin(User user)
        {
            user.LoginAttempts = 0;
            user.LastLogin = DateTime.UtcNow;
            DataContext.User.Save(user);
        }

        private string GetCustomHeader(HttpActionContext actionContext, string name)
        {
            IEnumerable<string> values;
            if (!actionContext.Request.Headers.TryGetValues(name, out values))
                return null;

            return values.First();
        }
    }
}