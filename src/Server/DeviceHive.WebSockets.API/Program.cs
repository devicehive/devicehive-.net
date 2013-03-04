using System;
using System.Collections.Generic;
using System.ServiceProcess;
using DeviceHive.WebSockets.API.Core;
using DeviceHive.WebSockets.API.Service;
using Ninject;
using log4net;

namespace DeviceHive.WebSockets.API
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // parse command line
            var options = new Dictionary<string, string>(args.Length / 2, StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < args.Length / 2; i++)
            {
                var key = args[i];
                var value = args[i + 1];

                options.Add(key, value);
            }

            // load alternative config
            string configPath;
            if (options.TryGetValue("-config", out configPath))
                AppConfigLoader.Load(configPath);

            // load app mode
            AppMode appMode;            
            string appModeStr;

            if (!options.TryGetValue("-mode", out appModeStr) ||
                !Enum.TryParse(appModeStr, true, out appMode))
            {
                appMode = AppMode.WindowsService;
            }

            log4net.Config.XmlConfigurator.Configure();
            System.Net.ServicePointManager.DefaultConnectionLimit = 1000;
            System.Threading.ThreadPool.SetMinThreads(1000, 1000);

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                LogManager.GetLogger(typeof(Program)).Fatal(
                    "Unhandled exception", (Exception)e.ExceptionObject);
            };

            using (var kernel = NinjectConfig.CreateKernel())
            {                
                // get controller instances now, otherwise deadlock is possible in Ninject
                kernel.Get<Controllers.DeviceController>();
                kernel.Get<Controllers.ClientController>();

                switch (appMode)
                {
                    case AppMode.HostedApp:
                        RunHostedApp(kernel);
                        break;

                    case AppMode.SelfHostApp:
                        RunSelfHostApp(kernel);
                        break;

                    case AppMode.WindowsService:
                        RunWindowsService(kernel);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static void RunHostedApp(IKernel kernel)
        {
            var service = kernel.Get<HostedAppServiceImpl>();
            service.Start();
            service.FinishedWaitHandle.WaitOne();
        }

        private static void RunSelfHostApp(IKernel kernel)
        {
            var service = kernel.Get<SelfHostServiceImpl>();

            Console.WriteLine("Press 'q' to quit");
            service.Start();

            while (true)
            {
                try
                {
                    var key = Console.ReadKey().KeyChar;
                    if (key == 'q' || key == 'Q')
                        break;
                }
                catch (InvalidOperationException)
                {
                    // ignore error if console isn't attached to process now
                }
            }

            service.Stop();
        }

        private static void RunWindowsService(IKernel kernel)
        {
            var service = kernel.Get<SelfHostServiceImpl>();
            ServiceBase.Run(new WindowsService(service));
        }

        private enum AppMode
        {
            HostedApp,
            SelfHostApp,
            WindowsService
        }
    }
}
