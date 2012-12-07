using System;
using DeviceHive.Test.Stubs;
using DeviceHive.Test.WebSocketsTest.Utils;
using DeviceHive.WebSockets.Network;
using NUnit.Framework;
using Ninject;

namespace DeviceHive.Test.WebSocketsTest
{
    public abstract class WebSocketTestBase : AssertionHelper
    {
        protected const string Login = "dhadmin";
        protected const string Password = "dhadmin_#911";

        protected static readonly Guid DeviceGUID = new Guid("a97266f4-6e8a-4008-8242-022b49ea484f");
        protected const string DeviceKey = "key";

        [SetUp]
        public void SetUp()
        {
            var kernel = NinjectConfig.CreateKernel();
            var server = (StubWebSocketServer) kernel.Get<WebSocketServerBase>();
            ClientController = new ClientControllerWrapper(server, kernel);
            DeviceController = new DeviceControllerWrapper(server, kernel);
        }

        protected ClientControllerWrapper ClientController { get; private set; }
        
        protected DeviceControllerWrapper DeviceController { get; private set; }
    }
}