using DeviceHive.WebSockets.API.Controllers;
using DeviceHive.WebSockets.Core.ActionsFramework;

namespace DeviceHive.WebSockets.API.Core
{
    public static class RoutesConfig
    {
        public static void ConfigureRoutes(Router router)
        {
            router.RegisterController("/client", typeof(ClientController));
            router.RegisterController("/device", typeof(DeviceController));
        }
    }
}