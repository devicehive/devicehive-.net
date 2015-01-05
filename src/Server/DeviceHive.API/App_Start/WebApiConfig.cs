using DeviceHive.API.Filters;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;

namespace DeviceHive.API
{
    public class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // attribute routing
            var constraintResolver = new DefaultInlineConstraintResolver();
            constraintResolver.ConstraintMap.Add("idorcurrent", typeof(IdOrCurrentConstraint));
            constraintResolver.ConstraintMap.Add("deviceGuid", typeof(DeviceGuidConstraint));
            config.MapHttpAttributeRoutes(constraintResolver);

            // use JSON by default
            var xmlMediaTypes = config.Formatters.XmlFormatter.SupportedMediaTypes;
            xmlMediaTypes.Remove(xmlMediaTypes.FirstOrDefault(t => t.MediaType == "application/xml"));

            // set proper JSON formatting
            var jsonFormatter = config.Formatters.JsonFormatter;
            jsonFormatter.SerializerSettings.Converters.Add(new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.ffffff" });

            // message handlers
            config.MessageHandlers.Add(new XHttpMethodDelegatingHandler());

            // action selector which handles options method
            config.Services.Replace(typeof(IHttpActionSelector), new ActionSelector());

            // global filters
            config.Filters.Add(new HandleExceptionAttribute());
            config.Filters.Add(new AuthenticationFilter());
            config.Filters.Add(new AllowCrossDomainOrigin());
        }
    }
}