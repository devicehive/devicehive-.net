using System;

namespace DeviceHive.WebSockets.Core.ActionsFramework
{
    public class WebSocketRequestException : Exception
    {
        public WebSocketRequestException(string message) : base(message)
        {
        }
    }
}