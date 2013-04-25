using System.Configuration;
using DeviceHive.WebSockets.Core.ActionsFramework;
using DeviceHive.WebSockets.Core.Hosting;

namespace DeviceHive.WebSockets.API.Service
{
    internal class HostedAppServiceImpl : ApplicationServiceBase
    {
        public HostedAppServiceImpl(Router router) : base(
            ConfigurationManager.AppSettings["webSocketsHostPipeName"],
            ConfigurationManager.AppSettings["webSocketsAppPipeName"])
        {
            ConnectionOpened += (s, e) => router.HandleNewConnection(e.Connection);
            MessageReceived += (s, e) => router.RouteRequest(e.Connection, e.Message);
            ConnectionClosed += (s, e) => router.CleanupConnection(e.Connection);
        }
    }
}