using System;
using System.Linq;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using DeviceHive.API.Filters;
using log4net;

namespace DeviceHive.API
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // configure logger
            log4net.Config.XmlConfigurator.Configure();

            // configure Web API
            FilterConfig.RegisterFilters(GlobalConfiguration.Configuration);
            RouteConfig.RegisterRoutes(GlobalConfiguration.Configuration.Routes);

            // use JSON by default
            var xmlMediaTypes = GlobalConfiguration.Configuration.Formatters.XmlFormatter.SupportedMediaTypes;
            xmlMediaTypes.Remove(xmlMediaTypes.FirstOrDefault(t => t.MediaType == "application/xml"));
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var exception = Server.GetLastError();
            LogManager.GetLogger("DeviceHive.API").Fatal("Application Error!", exception);
        }
    }
}