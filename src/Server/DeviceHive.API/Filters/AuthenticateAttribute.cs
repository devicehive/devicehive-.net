using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using DeviceHive.API.Controllers;
using DeviceHive.Core;
using DeviceHive.Data;
using DeviceHive.Data.Model;

namespace DeviceHive.API.Filters
{
    public class AuthenticateAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            // retrieve controller object
            var controller = (BaseController)actionContext.ControllerContext.Controller;

            // check basic authentication
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
                var user = controller.DataContext.User.Get(login);
                if (user != null && user.Status == (int)UserStatus.Active)
                {
                    // check user password
                    if (user.IsValidPassword(password))
                    {
                        // authenticate the user
                        UpdateUserLastLogin(controller, user);
                        controller.CallContext.CurrentUser = user;
                    }
                    else
                    {
                        IncrementUserLoginAttempts(controller, user);
                    }
                }
                
                return;
            }

            // check access key authentication
            if (auth != null && auth.Scheme == "Bearer" && !string.IsNullOrEmpty(auth.Parameter))
            {
                // get the token value
                var token = auth.Parameter;

                // get the access key object
                var accessKey = controller.DataContext.AccessKey.Get(token);
                if (accessKey != null && (accessKey.ExpirationDate == null || accessKey.ExpirationDate > DateTime.UtcNow))
                {
                    // get the user object
                    var user = controller.DataContext.User.Get(accessKey.UserID);
                    if (user != null && user.Status == (int)UserStatus.Active)
                    {
                        // authenticate the user
                        controller.CallContext.CurrentAccessKey = accessKey;
                        controller.CallContext.CurrentUser = user;
                    }
                }

                return;
            }

            // check device authentication
            var deviceId = GetCustomHeader(actionContext, "Auth-DeviceID");
            if (!string.IsNullOrEmpty(deviceId))
            {
                // get the device object
                var device = controller.DataContext.Device.Get(deviceId);
                if (device != null)
                {
                    // check device key authentication
                    var authDeviceKey = GetCustomHeader(actionContext, "Auth-DeviceKey");
                    if (authDeviceKey != null && device.Key == authDeviceKey)
                    {
                        // authenticate the device and update last online
                        controller.CallContext.CurrentDevice = device;
                        controller.DataContext.Device.SetLastOnline(device.ID);
                    }
                }

                return;
            }
        }

        private void IncrementUserLoginAttempts(BaseController controller, User user)
        {
            var maxLoginAttempts = controller.DeviceHiveConfiguration.UserPasswordPolicy.MaxLoginAttempts;

            user.LoginAttempts++;
            if (maxLoginAttempts > 0 && user.LoginAttempts >= maxLoginAttempts)
                user.Status = (int)UserStatus.LockedOut;
            controller.DataContext.User.Save(user);
        }

        private void UpdateUserLastLogin(BaseController controller, User user)
        {
            // update LastLogin only if it's too far behind - save database resources
            if (user.LoginAttempts > 0 || user.LastLogin == null || user.LastLogin.Value.AddHours(1) < DateTime.UtcNow)
            {
                user.LoginAttempts = 0;
                user.LastLogin = DateTime.UtcNow;
                controller.DataContext.User.Save(user);
            }
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