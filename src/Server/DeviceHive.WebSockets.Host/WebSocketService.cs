using System.ServiceProcess;

namespace DeviceHive.WebSockets.Host
{
    public class WebSocketService : ServiceBase
    {
        private readonly HostServiceImpl _impl;

        internal WebSocketService(HostServiceImpl impl)
        {
            _impl = impl;

            ServiceName = "DeviceHive.WebSockets.Host";
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            _impl.Start();
        }

        protected override void OnStop()
        {
            base.OnStop();
            _impl.Stop();
        }
    }
}