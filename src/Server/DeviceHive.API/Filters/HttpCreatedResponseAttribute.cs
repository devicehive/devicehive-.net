using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace DeviceHive.API.Filters
{
    public class HttpCreatedResponseAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            if (actionExecutedContext.Response != null && actionExecutedContext.Response.IsSuccessStatusCode)
            {
                // set HTTP 201 status
                actionExecutedContext.Response.StatusCode = HttpStatusCode.Created;

                // set Location header
                var result = JObject.Parse(await actionExecutedContext.Response.Content.ReadAsStringAsync());
                if (result["id"] != null)
                    actionExecutedContext.Response.Headers.Location = new Uri(actionExecutedContext.Request.RequestUri.AbsoluteUri + "/" + result["id"]);
            }
        }
    }
}