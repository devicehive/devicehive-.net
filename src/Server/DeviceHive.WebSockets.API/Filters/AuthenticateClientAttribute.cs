using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Core;
using DeviceHive.Data;
using DeviceHive.Data.Model;
using DeviceHive.WebSockets.Core.ActionsFramework;

namespace DeviceHive.WebSockets.API.Filters
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AuthenticateClientAttribute : ActionFilterAttribute
    {
        #region ActionFilterAttribute Members

        public override void OnAuthentication(ActionContext actionContext)
        {
            var request = actionContext.Request;
            if (request == null)
                return;

            var login = (string)request["login"];
            var password = (string)request["password"];
            var accessKey = (string)request["accessKey"];
            var controller = (DeviceHive.WebSockets.API.Controllers.ControllerBase)actionContext.Controller;

            if (login != null && password != null)
            {
                // get the user
                var user = controller.DataContext.User.Get(login);
                if (user == null || user.Status != (int)UserStatus.Active || !user.HasPassword())
                    throw new WebSocketRequestException("Invalid login or password");

                // verify user password
                if (user.IsValidPassword(password))
                {
                    UpdateUserLastLogin(controller.DataContext, user);
                }
                else
                {
                    IncrementUserLoginAttempts(controller.DataContext, controller.DeviceHiveConfiguration, user);
                    throw new WebSocketRequestException("Invalid login or password");
                }

                // authenticate the user
                actionContext.Parameters["AuthUser"] = user;
            }
            else if (accessKey != null)
            {
                // get the access key object
                var key = controller.DataContext.AccessKey.Get(accessKey);
                if (key == null || (key.ExpirationDate != null && key.ExpirationDate <= DateTime.UtcNow))
                    throw new WebSocketRequestException("Invalid access key");

                // get the user object
                var user = controller.DataContext.User.Get(key.UserID);
                if (user == null || user.Status != (int)UserStatus.Active)
                    throw new WebSocketRequestException("Invalid access key");

                // authenticate the user
                actionContext.Parameters["AuthAccessKey"] = key;
                actionContext.Parameters["AuthUser"] = user;
            }
        }
        
        #endregion

        #region Private Methods

        private void IncrementUserLoginAttempts(DataContext dataContext, DeviceHiveConfiguration configuration, User user)
        {
            var maxLoginAttempts = configuration.Authentication.MaxLoginAttempts;

            user.LoginAttempts++;
            if (maxLoginAttempts > 0 && user.LoginAttempts >= maxLoginAttempts)
                user.Status = (int)UserStatus.LockedOut;
            dataContext.User.Save(user);
        }

        private void UpdateUserLastLogin(DataContext dataContext, User user)
        {
            // update LastLogin only if it's too far behind - save database resources
            if (user.LoginAttempts > 0 || user.LastLogin == null || user.LastLogin.Value.AddHours(1) < DateTime.UtcNow)
            {
                user.LoginAttempts = 0;
                user.LastLogin = DateTime.UtcNow;
                dataContext.User.Save(user);
            }
        }

        #endregion
    }
}
