using DeviceHive.WebSockets.ActionsFramework;
using DeviceHive.WebSockets.Controllers;

namespace DeviceHive.WebSockets.Core
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