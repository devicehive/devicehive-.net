using System;

namespace DeviceHive.WebSockets.Core.ActionsFramework
{
    [Serializable]
    public class WebSocketRequestException : Exception
    {
        public WebSocketRequestException(string message) : base(message)
        {
        }
    }
}