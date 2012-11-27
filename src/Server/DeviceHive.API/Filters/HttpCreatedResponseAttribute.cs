using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http.Filters;
using System.Web.Http.Routing;
using Newtonsoft.Json.Linq;

namespace DeviceHive.API.Filters
{
    public class HttpCreatedResponseAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext context)
        {
            if (context.Response != null && context.Response.IsSuccessStatusCode)
            {
                context.Response.StatusCode = HttpStatusCode.Created;
                context.Response.Content.ReadAsStringAsync().ContinueWith(task =>
                    {
                        var result = JObject.Parse(task.Result);
                        if (result["id"] != null)
                        {
                            var controller = context.ActionContext.ControllerContext.ControllerDescriptor.ControllerName;
                            var route = new UrlHelper(context.Request).Route(null, new { controller = controller, id = result["id"] });
                            context.Response.Headers.Location = new Uri(context.Request.RequestUri, route);
                        }
                    }).Wait();
            }
        }
    }
}