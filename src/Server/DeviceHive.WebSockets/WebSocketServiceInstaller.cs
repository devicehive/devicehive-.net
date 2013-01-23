using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace DeviceHive.WebSockets
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
                    ServiceName = "DeviceHive.WebSockets 1.2",
                    DisplayName = "DeviceHive WebSockets Service 1.2",
                    Description = "DeviceHive WebSockets Service 1.2",
                    StartType = ServiceStartMode.Automatic
                }
            });
        }
    }
}
