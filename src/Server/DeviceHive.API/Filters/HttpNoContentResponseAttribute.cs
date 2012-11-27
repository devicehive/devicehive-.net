using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http.Filters;

namespace DeviceHive.API.Filters
{
    public class HttpNoContentResponseAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext context)
        {
            if (context.Response != null && context.Response.IsSuccessStatusCode)
            {
                context.Response.StatusCode = HttpStatusCode.NoContent;
            }
        }
    }
}