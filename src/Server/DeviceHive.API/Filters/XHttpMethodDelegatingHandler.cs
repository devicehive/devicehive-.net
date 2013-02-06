using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceHive.API.Filters
{
    public class XHttpMethodDelegatingHandler : DelegatingHandler
    {
        private static readonly string[] _allowedHttpMethods = { "PUT", "DELETE" };
        private static readonly string _httpMethodHeader = "X-HTTP-Method-Override";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post && request.Headers.Contains(_httpMethodHeader))
            {
                string httpMethod = request.Headers.GetValues(_httpMethodHeader).FirstOrDefault();
                if (_allowedHttpMethods.Contains(httpMethod, StringComparer.InvariantCultureIgnoreCase))
                    request.Method = new HttpMethod(httpMethod);
            }
            return base.SendAsync(request, cancellationToken);
        }
    }
}