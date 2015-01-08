using DeviceHive.API.Internal;
using DeviceHive.Core;
using System;
using System.Linq;
using System.Net;
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

            var address = Subnet.ParseAddress(actionContext.Request.GetUserAddress());
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
        #endregion
    }
}