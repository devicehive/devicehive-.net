using System;
using System.Linq;
using DeviceHive.Data;
using DeviceHive.Data.Model;
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

        protected static readonly Guid DeviceGUID = new Guid("6a887d33-03cc-40c4-bd33-ef5a7f495291");
        protected static readonly Guid OtherDeviceGUID = new Guid("b306bfdf-a1cf-4711-bfcb-36b825b3f778");
        protected const string DeviceKey = "key";

        [SetUp]
        public void SetUp()
        {
            var kernel = NinjectConfig.CreateKernel();
            var server = (StubWebSocketServer) kernel.Get<WebSocketServerBase>();
            ClientController = new ClientControllerWrapper(server, kernel);
            DeviceController = new DeviceControllerWrapper(server, kernel);

            var dataContext = kernel.Get<DataContext>();
            EnsureDeviceExists(dataContext, DeviceGUID);
            EnsureDeviceExists(dataContext, OtherDeviceGUID);
        }

        private void EnsureDeviceExists(DataContext dataContext, Guid deviceGuid)
        {
            var device = dataContext.Device.Get(deviceGuid);
            if (device != null)
                return;

            var network = dataContext.Network.GetAll().FirstOrDefault();
            var deviceClass = dataContext.DeviceClass.GetAll().FirstOrDefault();

            device = new Device(deviceGuid)
            {
                Name = "test device",
                Network = network,
                DeviceClass = deviceClass,
                Key = DeviceKey
            };

            dataContext.Device.Save(device);
        }

        protected ClientControllerWrapper ClientController { get; private set; }
        
        protected DeviceControllerWrapper DeviceController { get; private set; }
    }
}