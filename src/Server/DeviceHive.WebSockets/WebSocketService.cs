using System.ServiceProcess;

namespace DeviceHive.WebSockets
{
    public class WebSocketService : ServiceBase
    {
        private readonly WebSocketServiceImpl _impl;

        public WebSocketService(WebSocketServiceImpl impl)
        {            
            _impl = impl;

            ServiceName = "DeviceHive.WebSockets";
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