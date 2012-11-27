using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Filters;

namespace DeviceHive.API.Filters
{
    public class AllowCrossDomainOrigin : ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext context)
        {
            var origin = context.Request.Headers.FirstOrDefault(h => h.Key == "Origin");
            if (origin.Value != null && origin.Value.Any())
            {
                context.Response.Headers.Add("Access-Control-Allow-Origin", origin.Value.First());
                context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
                context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
                context.Response.Headers.Add("Access-Control-Allow-Headers", "Origin, Authorization, Accept");
            }
        }
    }
}