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
        private Subnet[] _allowedSubnets;

        [Ninject.Inject]
        public void Initialize(DeviceHiveConfiguration deviceHiveConfiguration)
        {
            _allowedSubnets = deviceHiveConfiguration.Maintenance.CronTriggerSubnets
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => Subnet.ParseSubnet(s)).ToArray();
        }

        #region AuthorizationFilterAttribute Members

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var address = Subnet.ParseAddress(actionContext.Request.GetUserAddress());
            if (!_allowedSubnets.Any(s => s.Includes(address)))
                throw new HttpResponseException(HttpStatusCode.Unauthorized);
        }
        #endregion
    }
}