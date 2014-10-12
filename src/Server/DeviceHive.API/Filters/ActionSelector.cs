using DeviceHive.API.Controllers;
using System.Net.Http;
using System.Reflection;
using System.Web.Http.Controllers;

namespace DeviceHive.API.Filters
{
    public class ActionSelector : ApiControllerActionSelector
    {
        private static MethodInfo _optionsActionMethod = typeof(BaseController).GetMethod("Options");

        public override HttpActionDescriptor SelectAction(HttpControllerContext controllerContext)
        {
            if (controllerContext.Request.Method == HttpMethod.Options)
                return new ReflectedHttpActionDescriptor(controllerContext.ControllerDescriptor, _optionsActionMethod);

            return base.SelectAction(controllerContext);
        }
    }
}