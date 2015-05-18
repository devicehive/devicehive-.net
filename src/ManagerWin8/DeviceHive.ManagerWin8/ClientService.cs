using DeviceHive.Client;
using DeviceHive.ManagerWin8.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceHive.ManagerWin8
{
    class ClientService : DeviceHiveClient
    {
        public class EmptyCloudSettingsException : Exception
        {
            public EmptyCloudSettingsException() : base("Cloud settings are empty.") { }
        }

        static ClientService current;

        ClientService(DeviceHiveConnectionInfo connectionInfo, IRestClient restClient)
            : base(connectionInfo, restClient)
        {
            // use only WebSocketChannel, not LongPollingChannel
            SetAvailableChannels(new Channel[] {
                new WebSocketChannel(connectionInfo, restClient)
            });
        }

        static ClientService()
        {
            Settings.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "CloudServerUrl"
                    || e.PropertyName == "CloudUsername"
                    || e.PropertyName == "CloudPassword")
                {
                    current = null;
                }
            };
        }

        public static ClientService Current
        {
            get
            {
                if (current == null)
                {
                    if (String.IsNullOrEmpty(Settings.Instance.CloudServerUrl) || String.IsNullOrEmpty(Settings.Instance.CloudUsername) || String.IsNullOrEmpty(Settings.Instance.CloudPassword))
                    {
                        throw new EmptyCloudSettingsException();
                    }
                    var connInfo = new DeviceHiveConnectionInfo(Settings.Instance.CloudServerUrl, Settings.Instance.CloudUsername, Settings.Instance.CloudPassword);
                    current = new ClientService(connInfo, new RestClient(connInfo));
                }
                return current;
            }
        }
    }
}
