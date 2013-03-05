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
        void RemoveApplication(string host);

        [OperationContract]
        void ChangeApplication(string host,
            string exePath = null, string commandLineArgs = null,
            string userName = null, string userPassword = null);

        [OperationContract]
        void StopApplication(string host);

        [OperationContract]
        void StartApplication(string host);
    }
}