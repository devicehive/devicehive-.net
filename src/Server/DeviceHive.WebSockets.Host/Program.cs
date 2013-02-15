using System;
using log4net;

namespace DeviceHive.WebSockets.Host
{
    internal class Program
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
        }
    }
}
