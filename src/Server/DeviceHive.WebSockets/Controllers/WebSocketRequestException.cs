using System;

namespace DeviceHive.WebSockets.Controllers
{
	public class WebSocketRequestException : Exception
	{
		public WebSocketRequestException(string message) : base(message)
		{
		}
	}
}