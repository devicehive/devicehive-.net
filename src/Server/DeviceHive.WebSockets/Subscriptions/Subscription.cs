using System;
using DeviceHive.WebSockets.Network;

namespace DeviceHive.WebSockets.Subscriptions
{
    public class Subscription
    {
        public Subscription(Guid? deviceGuid, WebSocketConnectionBase connection)
        {
            DeviceGuid = deviceGuid;
            Connection = connection;
        }

        public Guid? DeviceGuid { get; private set; }

        public WebSocketConnectionBase Connection { get; set; }
    }
}