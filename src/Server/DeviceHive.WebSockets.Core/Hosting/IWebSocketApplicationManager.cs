using System.Runtime.Serialization;
using System.ServiceModel;

namespace DeviceHive.WebSockets.Core.Hosting
{
    [ServiceContract]
    public interface IWebSocketApplicationManager
    {
        [OperationContract]
        void AddApplication(string host, string exePath, string commandLineArgs,
            string userName, string userPassword);

        [OperationContract]
        bool RemoveApplication(string host);

        [OperationContract]
        bool ChangeApplication(string host,
            string exePath = null, string commandLineArgs = null,
            string userName = null, string userPassword = null);

        [OperationContract]
        bool StopApplication(string host);

        [OperationContract]
        bool StartApplication(string host);
    }
}