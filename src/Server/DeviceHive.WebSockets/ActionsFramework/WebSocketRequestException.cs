using System;

namespace DeviceHive.WebSockets.ActionsFramework
{
    public class WebSocketRequestException : Exception
    {
        public WebSocketRequestException(string message) : base(message)
        {
        }
    }
}