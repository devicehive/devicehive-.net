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

        public void RemoveApplication(string host)
        {
            Channel.RemoveApplication(host);
        }

        public void ChangeApplication(string host,
            string exePath = null, string commandLineArgs = null,
            string userName = null, string userPassword = null)
        {
            Channel.ChangeApplication(host, exePath, commandLineArgs, userName, userPassword);
        }

        public void StopApplication(string host)
        {
            Channel.StopApplication(host);
        }

        public void StartApplication(string host)
        {
            Channel.StopApplication(host);
        }
    }
}