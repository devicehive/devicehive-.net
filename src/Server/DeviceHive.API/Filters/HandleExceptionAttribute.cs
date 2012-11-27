using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;
using DeviceHive.API.Mapping;
using DeviceHive.API.Models;
using log4net;

namespace DeviceHive.API.Filters
{
    public class HandleExceptionAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            if (context.Exception is JsonMapperException)
            {
                context.Response = context.Request.CreateResponse<ErrorDetail>
                    (HttpStatusCode.BadRequest, new ErrorDetail(context.Exception.Message));
                return;
            }

            LogManager.GetLogger("DeviceHive.API").Fatal("API Exception!", context.Exception);
        }
    }
}