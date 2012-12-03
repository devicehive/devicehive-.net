using System;
using System.Diagnostics;
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
		    AppDomain.CurrentDomain.UnhandledException += (s, e) =>
		    {
		        LogManager.GetLogger(typeof (Program)).Fatal(
		            "Unhandled exception", (Exception) e.ExceptionObject);
		    };

			using (var kernel = NinjectConfig.CreateKernel())
		    {
		        var service = kernel.Get<WebSocketServiceImpl>();

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
