using System;
using System.Linq;
using System.Net.Http.Formatting;
using System.Web.Http;
using Newtonsoft.Json.Converters;
using DeviceHive.API.Filters;

namespace DeviceHive.API
{
    public class FilterConfig
    {
        public static void RegisterFilters(HttpConfiguration configuration)
        {
            configuration.Filters.Add(new HandleExceptionAttribute());
            configuration.Filters.Add(new AuthenticateAttribute());
            configuration.Filters.Add(new AllowCrossDomainOrigin());

            var formatter = configuration.Formatters
                .Where(f => f.SupportedMediaTypes.Any(v => v.MediaType.Equals("application/json", StringComparison.CurrentCultureIgnoreCase)))
                .FirstOrDefault() as JsonMediaTypeFormatter;
            formatter.SerializerSettings.Converters.Add(new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.ffffff" });
        }
    }
}