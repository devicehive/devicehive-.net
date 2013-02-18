using System;
using DeviceHive.WebSockets.API.Core;
using Ninject;
using log4net;

namespace DeviceHive.WebSockets.API
{
    internal class Program
    {
        private static void Main()
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
                var service = kernel.Get<ServiceImpl>();

                // get controller instances now, otherwise deadlock is possible in Ninject
                kernel.Get<Controllers.DeviceController>();
                kernel.Get<Controllers.ClientController>();

                service.Start();
                service.FinishedWaitHandle.WaitOne();
            }
        }
    }
}
