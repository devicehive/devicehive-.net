using System;
using System.Linq;
using System.Net.Http.Formatting;
using System.Web.Http;
using DeviceHive.API.Filters;
using Newtonsoft.Json.Converters;

namespace DeviceHive.API
{
    public class FilterConfig
    {
        public static void RegisterFilters(HttpConfiguration configuration)
        {
            configuration.Filters.Add(new HandleExceptionAttribute());
            configuration.Filters.Add(new AuthenticateAttribute());
            configuration.Filters.Add(new AllowCrossDomainOrigin());

            configuration.MessageHandlers.Add(new XHttpMethodDelegatingHandler());

            var jsonFormatter = configuration.Formatters.JsonFormatter;
            jsonFormatter.SerializerSettings.Converters.Add(new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.ffffff" });
        }
    }
}