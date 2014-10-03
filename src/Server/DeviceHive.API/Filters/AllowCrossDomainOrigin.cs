using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http.Filters;

namespace DeviceHive.API.Filters
{
    public class AllowCrossDomainOrigin : ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext context)
        {
            AppendCorsHeaders(context.Request, context.Response);
        }

        public static void AppendCorsHeaders(HttpRequestMessage request, HttpResponseMessage response)
        {
            if (!request.Headers.Contains("Origin"))
                return;

            var origin = request.Headers.GetValues("Origin").First();
            response.Headers.Add("Access-Control-Allow-Origin", origin);
            response.Headers.Add("Access-Control-Allow-Credentials", "true");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
            response.Headers.Add("Access-Control-Allow-Headers", "Origin, Authorization, Accept, Content-Type, Auth-DeviceID, Auth-DeviceKey");
        }
    }
}