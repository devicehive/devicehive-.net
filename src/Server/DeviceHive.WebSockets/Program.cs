using System.ServiceProcess;
using DeviceHive.WebSockets.Core;
using Ninject;

namespace DeviceHive.WebSockets
{
	internal static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		private static void Main()
		{
			var kernel = NinjectConfig.CreateKernel();
			var service = kernel.Get<WebSocketService>();
			ServiceBase.Run(service);
		}
	}
}
