using DeviceHive.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace DeviceHive.API.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class AuthorizeCronTriggerAttribute : AuthorizationFilterAttribute
    {
        private static object _syncRoot = new object();
        private static Subnet[] _allowedSubnets;

        #region AuthorizationFilterAttribute Members

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            ReadAllowedSubnets(actionContext.RequestContext.Configuration);

            var clientIp = GetClientIp(actionContext.Request);
            if (clientIp == null)
                throw new Exception("Unable to determine client IP address!");

            var address = Subnet.ParseAddress(clientIp);
            if (!_allowedSubnets.Any(s => s.Includes(address)))
                throw new HttpResponseException(HttpStatusCode.Unauthorized);
        }
        #endregion

        #region Private Methods

        private void ReadAllowedSubnets(HttpConfiguration httpConfiguration)
        {
            if (_allowedSubnets != null)
                return;

            lock (_syncRoot)
            {
                if (_allowedSubnets != null)
                    return;

                var configuration = (DeviceHiveConfiguration)httpConfiguration.DependencyResolver.GetService(typeof(DeviceHiveConfiguration));
                _allowedSubnets = configuration.Maintenance.CronTriggerSubnets.Split(',').Select(s => Subnet.ParseSubnet(s)).ToArray();
            }
        }

        private string GetClientIp(HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }

            return null;
        }
        #endregion
    }
}