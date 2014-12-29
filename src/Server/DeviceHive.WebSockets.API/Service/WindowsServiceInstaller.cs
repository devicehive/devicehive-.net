using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace DeviceHive.WebSockets.API.Service
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
                    Account = ServiceAccount.LocalSystem,
                    Username = null,
                    Password = null
                },
                new ServiceInstaller()
                {
                    ServiceName = "DeviceHive.WebSockets 1.3.1",
                    DisplayName = "DeviceHive WebSockets Service 1.3.1",
                    Description = "DeviceHive WebSockets Service 1.3.1",
                    StartType = ServiceStartMode.Automatic
                }
            });
        }
    }
}