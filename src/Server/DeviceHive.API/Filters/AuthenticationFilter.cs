using DeviceHive.API.Controllers;
using DeviceHive.API.Internal;
using DeviceHive.Core;
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
        private readonly DeviceHiveConfiguration _deviceHiveConfiguration;
        private readonly IAuthenticationManager _authenticationManager;

        public AuthenticationFilter(DeviceHiveConfiguration deviceHiveConfiguration, IAuthenticationManager authenticationManager)
        {
            _deviceHiveConfiguration = deviceHiveConfiguration;
            _authenticationManager = authenticationManager;
        }

        #region IAuthenticationFilter Members

        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            // retrieve controller object
            var controller = context.ActionContext.ControllerContext.Controller as BaseController;
            if (controller == null)
                throw new InvalidOperationException("Controller must inherit from BaseController class!");

            // check basic authentication
            var auth = context.Request.Headers.Authorization;
            if (auth != null && auth.Scheme == "Basic" && !string.IsNullOrEmpty(auth.Parameter))
            {
                // parse the authorization header
                string login, password;
                if (ParseBasicAuthParameter(auth.Parameter, out login, out password))
                {
                    // authenticate by password
                    try
                    {
                        controller.CallContext.CurrentUser = await _authenticationManager.AuthenticateByPasswordAsync(login, password);
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
                        // prolongate the key
                        if (accessKey.Type == (int)AccessKeyType.Session && accessKey.ExpirationDate != null &&
                            (accessKey.ExpirationDate.Value - DateTime.UtcNow).TotalSeconds < _deviceHiveConfiguration.Authentication.SessionTimeout.TotalSeconds / 2)
                        {
                            accessKey.ExpirationDate = DateTime.UtcNow.Add(_deviceHiveConfiguration.Authentication.SessionTimeout);
                            controller.DataContext.AccessKey.Save(accessKey);
                        }

                        // authenticate the user
                        controller.CallContext.CurrentAccessKey = accessKey;
                        controller.CallContext.CurrentUser = user;
                    }
                }

                return;
            }

            // check device authentication
            var deviceId = context.Request.GetCustomHeader("Auth-DeviceID");
            if (!string.IsNullOrEmpty(deviceId))
            {
                // get the device object
                var device = controller.DataContext.Device.Get(deviceId);
                if (device != null && device.Key != null)
                {
                    // check device key authentication
                    var authDeviceKey = context.Request.GetCustomHeader("Auth-DeviceKey");
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