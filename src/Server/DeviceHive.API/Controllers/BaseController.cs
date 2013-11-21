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
        protected internal RequestContext RequestContext { get; private set; }
        protected internal JsonMapperManager JsonMapperManager { get; private set; }

        #region Public Methods

        [Inject]
        [NonAction]
        public void Initialize(DataContext dataContext, RequestContext requestContext, JsonMapperManager jsonMapperManager)
        {
            DataContext = dataContext;
            RequestContext = requestContext;
            JsonMapperManager = jsonMapperManager;
        }

        [HttpNoContentResponse]
        [ApiExplorerSettings(IgnoreApi = true)]
        public void Options()
        {
        }
        #endregion

        #region Protected Methods

        protected void EnsureUserAccessTo(int userId)
        {
            if (RequestContext.CurrentUser.Role != (int)UserRole.Administrator && RequestContext.CurrentUser.ID != userId)
                ThrowHttpResponse(HttpStatusCode.Unauthorized, "Not authorized");
        }

        protected void EnsureDeviceAccess(Guid deviceGuid)
        {
            if (RequestContext.CurrentDevice == null)
                return;

            if (RequestContext.CurrentDevice.GUID != deviceGuid)
                ThrowHttpResponse(HttpStatusCode.Unauthorized, "Not authorized");
        }

        protected bool IsNetworkAccessible(Network network)
        {
            if (RequestContext.CurrentUser == null)
                return true;

            if (RequestContext.CurrentUser.Role != (int)UserRole.Administrator)
            {
                if (RequestContext.CurrentUserNetworks == null)
                    RequestContext.CurrentUserNetworks = DataContext.UserNetwork.GetByUser(RequestContext.CurrentUser.ID);

                if (!RequestContext.CurrentUserNetworks.Any(un => un.NetworkID == network.ID))
                    return false;
            }

            return RequestContext.CurrentUserPermissions == null ||
                RequestContext.CurrentUserPermissions.Any(p => p.IsNetworkAllowed(network.ID));
        }

        protected bool IsDeviceAccessible(Device device)
        {
            if (RequestContext.CurrentUser == null)
                return true;

            if (RequestContext.CurrentUser.Role != (int)UserRole.Administrator)
            {
                if (device.NetworkID == null)
                    return false;

                if (RequestContext.CurrentUserNetworks == null)
                    RequestContext.CurrentUserNetworks = DataContext.UserNetwork.GetByUser(RequestContext.CurrentUser.ID);

                if (!RequestContext.CurrentUserNetworks.Any(un => un.NetworkID == device.NetworkID))
                    return false;
            }

            return RequestContext.CurrentUserPermissions == null || RequestContext.CurrentUserPermissions.Any(p =>
                p.IsNetworkAllowed(device.NetworkID) && p.IsDeviceAllowed(device.GUID.ToString()));
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

        protected Task Delay(int timeout)
        {
            var taskSource = new TaskCompletionSource<bool>();
            new Timer(self =>
                {
                    ((IDisposable)self).Dispose();
                    taskSource.TrySetResult(true);
                }).Change(timeout, Timeout.Infinite);
            return taskSource.Task;
        }
        #endregion
    }
}