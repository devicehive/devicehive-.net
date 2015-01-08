using DeviceHive.API.Models;
using DeviceHive.Core.Mapping;
using log4net;
using System.Net;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;
using System.Web.Http.Filters;

namespace DeviceHive.API.Filters
{
    public class HandleExceptionAttribute : ExceptionFilterAttribute
    {
        private static bool _isCustomError;

        static HandleExceptionAttribute()
        {
            var ceSection = (CustomErrorsSection)WebConfigurationManager.OpenWebConfiguration("~").GetSection("system.web/customErrors");
            _isCustomError = ceSection.Mode != CustomErrorsMode.Off;
        }

        public override void OnException(HttpActionExecutedContext context)
        {
            if (context.Exception is HttpResponseException)
                return;

            if (context.Exception is JsonMapperException)
            {
                context.Response = context.Request.CreateResponse<ErrorDetail>
                    (HttpStatusCode.BadRequest, new ErrorDetail(context.Exception.Message));
                return;
            }

            LogManager.GetLogger("DeviceHive.API").Fatal("API Exception!", context.Exception);
            context.Response = context.Request.CreateResponse<ErrorDetail>(HttpStatusCode.InternalServerError,
                new ErrorDetail(_isCustomError ? "An error has occured with the server" : context.Exception.ToString()));
        }
    }
}