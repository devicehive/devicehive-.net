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
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // configure logger
            log4net.Config.XmlConfigurator.Configure();

            // configure Web API
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var exception = Server.GetLastError();
            if (exception is OperationCanceledException)
                return;

            LogManager.GetLogger("DeviceHive.API").Fatal("Application Error!", exception);
        }
    }
}