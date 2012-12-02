using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using DeviceHive.API.Filters;
using DeviceHive.API.Models;
using DeviceHive.Core.Mapping;
using DeviceHive.Data;
using DeviceHive.Data.Model;
using Ninject;

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

        protected void EnsureDeviceAccess(Guid deviceGuid)
        {
            if (RequestContext.CurrentDevice == null)
                return;

            if (RequestContext.CurrentDevice.GUID != deviceGuid)
                ThrowHttpResponse(HttpStatusCode.Unauthorized, "Not authorized");
        }

        protected bool IsNetworkAccessible(int? networkId)
        {
            if (RequestContext.CurrentUser == null)
                return true;

            if (RequestContext.CurrentUser.Role == (int)UserRole.Administrator)
                return true;

            if (networkId == null)
                return false;

            var userNetworks = DataContext.UserNetwork.GetByUser(RequestContext.CurrentUser.ID);
            return userNetworks.Any(un => un.NetworkID == networkId);
        }

        protected IJsonMapper<T> GetMapper<T>()
        {
            return JsonMapperManager.GetMapper<T>();
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