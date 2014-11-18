using System;
using System.Linq;
using DeviceHive.Data;
using DeviceHive.Data.Model;
using DeviceHive.Test.Stubs;
using DeviceHive.Test.WebSocketsTest.Utils;
using DeviceHive.WebSockets.Core.Network;
using NUnit.Framework;
using Ninject;

namespace DeviceHive.Test.WebSocketsTest
{
    public abstract class WebSocketTestBase : AssertionHelper
    {
        protected const string Login = "dhadmin";
        protected const string Password = "dhadmin_#911";

        protected static readonly string DeviceGUID = "6a887d33-03cc-40c4-bd33-ef5a7f495291";
        protected static readonly string OtherDeviceGUID = "b306bfdf-a1cf-4711-bfcb-36b825b3f778";
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

        private void EnsureDeviceExists(DataContext dataContext, string deviceGuid)
        {
            var device = dataContext.Device.Get(deviceGuid);
            if (device != null)
            {
                if (device.Name == "test device")
                    return;
                
                device.Name = "test device";
                dataContext.Device.Save(device);
                return;
            }

            device = new Device(deviceGuid)
            {
                Name = "test device",
                Network = GetNetwork(dataContext),
                DeviceClass = GetDeviceClass(dataContext),
                Key = DeviceKey
            };

            dataContext.Device.Save(device);
        }

        private Network GetNetwork(DataContext dataContext)
        {
            var network = dataContext.Network.Get("test network");
            if (network != null)
                return network;
                        
            network = new Network("test network");
            dataContext.Network.Save(network);
            return network;
        }

        private DeviceClass GetDeviceClass(DataContext dataContext)
        {
            var deviceClass = dataContext.DeviceClass.Get("device class", "1");
            if (deviceClass != null)
                return deviceClass;

            deviceClass = new DeviceClass("device class", "1");
            dataContext.DeviceClass.Save(deviceClass);
            return deviceClass;
        }

        protected ClientControllerWrapper ClientController { get; private set; }
        
        protected DeviceControllerWrapper DeviceController { get; private set; }
    }
}