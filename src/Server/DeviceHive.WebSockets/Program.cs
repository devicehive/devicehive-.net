using System;
using System.ServiceProcess;
using DeviceHive.WebSockets.Core;
using Ninject;
using log4net;

namespace DeviceHive.WebSockets
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main(string[] args)
        {
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
                var service = kernel.Get<WebSocketServiceImpl>();

                // get controller instances now, otherwise deadlock is possible in Ninject
                kernel.Get<DeviceHive.WebSockets.Controllers.DeviceController>();
                kernel.Get<DeviceHive.WebSockets.Controllers.ClientController>();

                if (args.Length > 0 && args[0] == "-console")
                {
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
                else
                {
                    ServiceBase.Run(new WebSocketService(service));
                }
            }
        }
    }
}
