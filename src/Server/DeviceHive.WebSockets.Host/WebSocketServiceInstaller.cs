using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace DeviceHive.WebSockets.Host
{
    [RunInstaller(true)]
    public class WebSocketServiceInstaller : Installer
    {
        public WebSocketServiceInstaller()
        {
            Installers.AddRange(new Installer[]
            {
                new ServiceProcessInstaller()
                {
                    Account = ServiceAccount.LocalService,
                    Username = null,
                    Password = null
                },
                new ServiceInstaller()
                {
                    ServiceName = "DeviceHive.WebSockets.Host 2.0.0",
                    DisplayName = "DeviceHive WebSockets Host Service 2.0.0",
                    Description = "DeviceHive WebSockets Host Service 2.0.0",
                    StartType = ServiceStartMode.Automatic
                }
            });
        }
    }
}