using System.ServiceModel;

namespace DeviceHive.WebSockets.Core.Hosting
{
    public class WebSocketApplicationManagerClient : ClientBase<IWebSocketApplicationManager>,
        IWebSocketApplicationManager
    {
        public void AddApplication(string host, string exePath, string commandLineArgs,
            string userName, string userPassword)
        {
            Channel.AddApplication(host, exePath, commandLineArgs, userName, userPassword);
        }

        public bool RemoveApplication(string host)
        {
            return Channel.RemoveApplication(host);
        }

        public bool ChangeApplication(string host,
            string exePath = null, string commandLineArgs = null,
            string userName = null, string userPassword = null)
        {
            return Channel.ChangeApplication(host, exePath, commandLineArgs, userName, userPassword);
        }

        public bool StopApplication(string host)
        {
            return Channel.StopApplication(host);
        }

        public bool StartApplication(string host)
        {
            return Channel.StopApplication(host);
        }
    }
}