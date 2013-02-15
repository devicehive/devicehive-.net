using System.Collections.Generic;
using DeviceHive.WebSockets.Core.Hosting;

namespace DeviceHive.WebSockets.Host
{
    internal class ApplicationCollection
    {
        private readonly object _lock = new object();

        private readonly Dictionary<string, ApplicationInfo> _applicationsByHost =
            new Dictionary<string, ApplicationInfo>();


        public void SendMessage<TMessage>(string host, TMessage message)
        {
            lock (_lock)
            {
                ApplicationInfo applicationInfo;
                if (!_applicationsByHost.TryGetValue(host, out applicationInfo))
                    return;

                //if (applicationInfo.State)
            }
        }
    }
}