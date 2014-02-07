using System.Configuration;
using DeviceHive.WebSockets.Core.ActionsFramework;
using DeviceHive.WebSockets.Core.Hosting;
using DeviceHive.Core;

namespace DeviceHive.WebSockets.API.Service
{
    internal class HostedAppServiceImpl
    {
        private readonly ApplicationService _service;

        public HostedAppServiceImpl(DeviceHiveConfiguration configuration, Router router)
        {
            var hosting = configuration.WebSocketEndpointHosting;
            if (string.IsNullOrEmpty(hosting.HostPipeName) || string.IsNullOrEmpty(hosting.AppPipeName))
                throw new ConfigurationErrorsException("Please specify hostPipeName and appPipeName in the webSocketEndpointHosting configuration element!");

            _service = new ApplicationService(hosting.HostPipeName, hosting.AppPipeName);
            _service.ConnectionOpened += (s, e) => router.HandleNewConnection(e.Connection);
            _service.MessageReceived += (s, e) => router.RouteRequest(e.Connection, e.Message);
            _service.ConnectionClosed += (s, e) => router.CleanupConnection(e.Connection);
        }

        public void Run()
        {
            _service.Start();
            _service.FinishedWaitHandle.WaitOne();
        }
    }
}