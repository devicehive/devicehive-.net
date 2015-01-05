using DeviceHive.API.Controllers;
using DeviceHive.Core.Authentication;
using DeviceHive.Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace DeviceHive.API.Filters
{
    public class AuthenticationFilter : IAuthenticationFilter
    {
        #region IAuthenticationFilter Members

        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            // retrieve controller object
            var controller = (BaseController)context.ActionContext.ControllerContext.Controller;
            var dependencyResolver = controller.Configuration.DependencyResolver;

            // check basic authentication
            var auth = context.Request.Headers.Authorization;
            if (auth != null && auth.Scheme == "Basic" && !string.IsNullOrEmpty(auth.Parameter))
            {
                // parse the authorization header
                string login, password;
                if (ParseBasicAuthParameter(auth.Parameter, out login, out password))
                {
                    // authenticate by password
                    var authenticationManager = (IAuthenticationManager)dependencyResolver.GetService(typeof(IAuthenticationManager));
                    try
                    {
                        controller.CallContext.CurrentUser = await authenticationManager.AuthenticateByPasswordAsync(login, password);
                    }
                    catch (AuthenticationException)
                    {
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
            var deviceId = GetCustomHeader(context.Request, "Auth-DeviceID");
            if (!string.IsNullOrEmpty(deviceId))
            {
                // get the device object
                var device = controller.DataContext.Device.Get(deviceId);
                if (device != null)
                {
                    // check device key authentication
                    var authDeviceKey = GetCustomHeader(context.Request, "Auth-DeviceKey");
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

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public bool AllowMultiple
        {
            get { return false; }
        }
        #endregion

        #region Private Methods

        private string GetCustomHeader(HttpRequestMessage request, string name)
        {
            IEnumerable<string> values;
            if (!request.Headers.TryGetValues(name, out values))
                return null;

            return values.First();
        }

        private bool ParseBasicAuthParameter(string parameter, out string login, out string password)
        {
            login = null;
            password = null;

            var authParam = (string)null;
            try
            {
                authParam = Encoding.UTF8.GetString(Convert.FromBase64String(parameter));
            }
            catch (FormatException)
            {
                return false;
            }

            if (!authParam.Contains(":") || authParam.IndexOf(":") == 0)
                return false;

            login = authParam.Substring(0, authParam.IndexOf(':'));
            password = authParam.Substring(authParam.IndexOf(':') + 1);
            return true;
        }
        #endregion
    }
}