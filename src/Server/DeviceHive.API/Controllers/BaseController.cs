using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using DeviceHive.API.Filters;
using DeviceHive.API.Models;
using DeviceHive.Core;
using DeviceHive.Core.Mapping;
using DeviceHive.Data;
using DeviceHive.Data.Model;
using Ninject;
using Newtonsoft.Json.Linq;

namespace DeviceHive.API.Controllers
{
    public class BaseController : ApiController
    {
        protected internal DataContext DataContext { get; private set; }
        protected internal CallContext CallContext { get; private set; }
        protected internal JsonMapperManager JsonMapperManager { get; private set; }
        protected internal DeviceHiveConfiguration DeviceHiveConfiguration { get; private set; }

        #region Public Methods

        [Inject]
        [NonAction]
        public void Initialize(DataContext dataContext, CallContext callContext, JsonMapperManager jsonMapperManager, DeviceHiveConfiguration deviceHiveConfiguration)
        {
            DataContext = dataContext;
            CallContext = callContext;
            JsonMapperManager = jsonMapperManager;
            DeviceHiveConfiguration = deviceHiveConfiguration;
        }

        [HttpNoContentResponse]
        [ApiExplorerSettings(IgnoreApi = true)]
        public void Options()
        {
        }
        #endregion

        #region Protected Methods

        protected void EnsureDeviceAccess(string deviceGuid)
        {
            if (CallContext.CurrentDevice == null)
                return;

            if (!string.Equals(CallContext.CurrentDevice.GUID, deviceGuid, StringComparison.OrdinalIgnoreCase))
                ThrowHttpResponse(HttpStatusCode.Unauthorized, "Not authorized");
        }

        protected bool IsNetworkAccessible(Network network)
        {
            if (CallContext.CurrentUser == null)
                return true;

            if (CallContext.CurrentUser.Role != (int)UserRole.Administrator)
            {
                if (CallContext.CurrentUserNetworks == null)
                    CallContext.CurrentUserNetworks = DataContext.UserNetwork.GetByUser(CallContext.CurrentUser.ID);

                if (!CallContext.CurrentUserNetworks.Any(un => un.NetworkID == network.ID))
                    return false;
            }

            return CallContext.CurrentUserPermissions == null ||
                CallContext.CurrentUserPermissions.Any(p => p.IsNetworkAllowed(network.ID));
        }

        protected bool IsDeviceAccessible(Device device)
        {
            if (CallContext.CurrentUser == null)
                return true;

            if (CallContext.CurrentUser.Role != (int)UserRole.Administrator)
            {
                if (device.NetworkID == null)
                    return false;

                if (CallContext.CurrentUserNetworks == null)
                    CallContext.CurrentUserNetworks = DataContext.UserNetwork.GetByUser(CallContext.CurrentUser.ID);

                if (!CallContext.CurrentUserNetworks.Any(un => un.NetworkID == device.NetworkID))
                    return false;
            }

            return CallContext.CurrentUserPermissions == null || CallContext.CurrentUserPermissions.Any(p =>
                p.IsNetworkAllowed(device.NetworkID) && p.IsDeviceAllowed(device.GUID));
        }

        protected IJsonMapper<T> GetMapper<T>()
        {
            return JsonMapperManager.GetMapper<T>();
        }

        protected T MapObjectFromQuery<T>()
        {
            var json = new JObject(Request.GetQueryNameValuePairs().Select(p => new JProperty(p.Key, p.Value)));
            return GetMapper<T>().Map(json);
        }

        protected void Validate(object entity)
        {
            var result = new List<ValidationResult>();
            if (!Validator.TryValidateObject(entity, new ValidationContext(entity, null, null), result, true))
            {
                ThrowHttpResponse(HttpStatusCode.BadRequest, result.First().ErrorMessage);
            }
        }

        protected HttpResponseMessage HttpResponse(HttpStatusCode code, string message)
        {
            return Request.CreateResponse<ErrorDetail>(code, new ErrorDetail(message));
        }

        protected void ThrowHttpResponse(HttpStatusCode status, string message)
        {
            throw new HttpResponseException(HttpResponse(status, message));
        }
        #endregion
    }
}