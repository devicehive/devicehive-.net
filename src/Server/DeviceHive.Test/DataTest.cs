using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeviceHive.Data.Model;
using DeviceHive.Data.EF;
using DeviceHive.Data.Repositories;
using NUnit.Framework;

namespace DeviceHive.Test
{
    [TestFixture]
    public class DataTest : RollbackTest
    {
        [Test]
        public void UserTest()
        {
            var user = new User("Test", "TestPass", (int)UserRole.Administrator, (int)UserStatus.Active);
            UserRepository.Save(user);

            // test GetAll
            var users = UserRepository.GetAll();
            Assert.Greater(users.Count, 0);

            // test Get(id)
            var user1 = UserRepository.Get(user.ID);
            Assert.IsNotNull(user1);
            Assert.AreEqual("Test", user1.Login);
            Assert.IsTrue(user1.IsValidPassword("TestPass"));
            Assert.IsFalse(user1.IsValidPassword("TestPass2"));
            Assert.AreEqual((int)UserRole.Administrator, user1.Role);
            Assert.AreEqual((int)UserStatus.Active, user1.Status);

            // test Get(name)
            var user2 = UserRepository.Get("Test");
            Assert.IsNotNull(user2);

            // test Save
            user.Login = "Test2";
            user.SetPassword("TestPass2");
            user.Role = (int)UserRole.Client;
            user.Status = (int)UserStatus.Disabled;
            user.LastLogin = DateTime.UtcNow;
            user.LoginAttempts = 1;
            UserRepository.Save(user);
            var user3 = UserRepository.Get(user.ID);
            Assert.AreEqual("Test2", user3.Login);
            Assert.IsTrue(user3.IsValidPassword("TestPass2"));
            Assert.AreEqual((int)UserRole.Client, user3.Role);
            Assert.AreEqual((int)UserStatus.Disabled, user3.Status);
            Assert.IsNotNull(user3.LastLogin);
            Assert.AreEqual(1, user3.LoginAttempts);

            // test Delete
            UserRepository.Delete(user.ID);
            var user4 = UserRepository.Get(user.ID);
            Assert.IsNull(user4);
        }

        [Test]
        public void UserNetworkTest()
        {
            var user = new User("Test", "TestPass", (int)UserRole.Administrator, (int)UserStatus.Active);
            UserRepository.Save(user);

            var network = new Network("Test");
            NetworkRepository.Save(network);

            var userNetwork = new UserNetwork(user, network);
            UserNetworkRepository.Save(userNetwork);

            // test GetByUser
            var userNetworks1 = UserNetworkRepository.GetByUser(user.ID);
            Assert.Greater(userNetworks1.Count, 0);

            // test GetByNetwork
            var userNetworks2 = UserNetworkRepository.GetByNetwork(network.ID);
            Assert.Greater(userNetworks2.Count, 0);

            // test Get(id)
            var userNetwork1 = UserNetworkRepository.Get(userNetwork.ID);
            Assert.IsNotNull(userNetwork1);
            Assert.AreEqual(user.ID, userNetwork1.UserID);
            Assert.AreEqual(network.ID, userNetwork1.NetworkID);
            Assert.IsNotNull(userNetwork1.User);
            Assert.IsNotNull(userNetwork1.Network);

            // test Get(userId, networkId)
            var userNetwork2 = UserNetworkRepository.Get(user.ID, network.ID);
            Assert.IsNotNull(userNetwork2);

            // test Delete
            UserNetworkRepository.Delete(userNetwork.ID);
            var userNetwork3 = UserNetworkRepository.Get(userNetwork.ID);
            Assert.IsNull(userNetwork3);
        }

        [Test]
        public void NetworkTest()
        {
            var network = new Network("Test") { Key = "Key" };
            NetworkRepository.Save(network);

            // test GetAll
            var networks = NetworkRepository.GetAll();
            Assert.Greater(networks.Count, 0);

            // test Get(id)
            var network1 = NetworkRepository.Get(network.ID);
            Assert.IsNotNull(network1);
            Assert.AreEqual("Test", network1.Name);

            // test Get(name)
            var network2 = NetworkRepository.Get("Test");
            Assert.IsNotNull(network2);

            // test Save
            network.Name = "Test2";
            network.Description = "Desc";
            NetworkRepository.Save(network);
            var network4 = NetworkRepository.Get(network.ID);
            Assert.AreEqual("Test2", network4.Name);
            Assert.AreEqual("Desc", network4.Description);

            // test Delete
            NetworkRepository.Delete(network.ID);
            var network5 = NetworkRepository.Get(network.ID);
            Assert.IsNull(network5);
        }

        [Test]
        public void DeviceClassTest()
        {
            var deviceClass = new DeviceClass("Test", "V1");
            DeviceClassRepository.Save(deviceClass);

            // test GetAll
            var deviceClasses = DeviceClassRepository.GetAll();
            Assert.Greater(deviceClasses.Count, 0);

            // test Get(id)
            var deviceClass1 = DeviceClassRepository.Get(deviceClass.ID);
            Assert.IsNotNull(deviceClass1);
            Assert.AreEqual("Test", deviceClass1.Name);

            // test Get(name, version)
            var deviceClass2 = DeviceClassRepository.Get("Test", "V1");
            Assert.IsNotNull(deviceClass2);

            // test Save
            deviceClass.Name = "Test2";
            deviceClass.Version = "V2";
            deviceClass.IsPermanent = true;
            deviceClass.OfflineTimeout = 10;
            deviceClass.Data = "{}";
            DeviceClassRepository.Save(deviceClass);
            var deviceClass3 = DeviceClassRepository.Get(deviceClass.ID);
            Assert.AreEqual("Test2", deviceClass3.Name);
            Assert.AreEqual("V2", deviceClass3.Version);
            Assert.AreEqual(true, deviceClass3.IsPermanent);
            Assert.AreEqual(10, deviceClass3.OfflineTimeout);
            Assert.AreEqual("{}", deviceClass3.Data);

            // test Delete
            DeviceClassRepository.Delete(deviceClass.ID);
            var deviceClass4 = DeviceClassRepository.Get(deviceClass.ID);
            Assert.IsNull(deviceClass4);
        }

        [Test]
        public void EquipmentTest()
        {
            var deviceClass = new DeviceClass("D1", "V1");
            DeviceClassRepository.Save(deviceClass);

            var equipment = new Equipment("Test", "Code", "Type", deviceClass);
            EquipmentRepository.Save(equipment);

            // test GetByDeviceClass
            var equipments = EquipmentRepository.GetByDeviceClass(deviceClass.ID);
            Assert.Greater(equipments.Count, 0);

            // test Get(id)
            var equipment1 = EquipmentRepository.Get(equipment.ID);
            Assert.IsNotNull(equipment1);
            Assert.AreEqual("Test", equipment1.Name);
            Assert.AreEqual("Code", equipment1.Code);
            Assert.AreEqual("Type", equipment1.Type);
            Assert.AreEqual(deviceClass.ID, equipment1.DeviceClassID);
            Assert.IsNotNull(equipment1.DeviceClass);

            // test Save
            equipment.Name = "Test2";
            equipment.Code = "Code2";
            equipment.Type = "Type2";
            equipment.Data = "{}";
            EquipmentRepository.Save(equipment);
            var equipment2 = EquipmentRepository.Get(equipment.ID);
            Assert.AreEqual("Test2", equipment2.Name);
            Assert.AreEqual("Code2", equipment2.Code);
            Assert.AreEqual("Type2", equipment2.Type);
            Assert.AreEqual("{}", equipment2.Data);

            // test update relationship
            var deviceClass2 = new DeviceClass("D2", "V2");
            DeviceClassRepository.Save(deviceClass2);
            equipment.DeviceClass = deviceClass2;
            EquipmentRepository.Save(equipment);
            var equipment3 = EquipmentRepository.Get(equipment.ID);
            Assert.AreEqual(deviceClass2.ID, equipment3.DeviceClassID);
            Assert.IsNotNull(equipment3.DeviceClass);

            // test Delete
            EquipmentRepository.Delete(equipment.ID);
            var equipment4 = EquipmentRepository.Get(equipment.ID);
            Assert.IsNull(equipment4);
        }

        [Test]
        public void DeviceTest()
        {
            var network = new Network("N1");
            NetworkRepository.Save(network);

            var deviceClass = new DeviceClass("D1", "V1");
            DeviceClassRepository.Save(deviceClass);

            var device = new Device(Guid.NewGuid(), "key", "Test", network, deviceClass);
            DeviceRepository.Save(device);

            // test GetByNetwork
            var devices = DeviceRepository.GetByNetwork(network.ID);
            Assert.Greater(devices.Count, 0);

            // test Get(id)
            var device1 = DeviceRepository.Get(device.ID);
            Assert.IsNotNull(device1);
            Assert.AreEqual(device.GUID, device1.GUID);
            Assert.AreEqual("Test", device1.Name);
            Assert.AreEqual(network.ID, device1.NetworkID);
            Assert.AreEqual(deviceClass.ID, device1.DeviceClassID);
            Assert.IsNotNull(device1.Network);
            Assert.IsNotNull(device1.DeviceClass);

            // test Get(guid)
            var device2 = DeviceRepository.Get(device.GUID);
            Assert.IsNotNull(device2);
            Assert.AreEqual(device.GUID, device2.GUID);
            Assert.AreEqual("Test", device2.Name);
            Assert.AreEqual(network.ID, device2.NetworkID);
            Assert.AreEqual(deviceClass.ID, device2.DeviceClassID);
            Assert.IsNotNull(device2.Network);
            Assert.IsNotNull(device2.DeviceClass);

            // test Save
            device.Name = "Test2";
            device.Status = "Status";
            device.Data = "{}";
            device.Network = null;
            device.NetworkID = null;
            DeviceRepository.Save(device);
            var device3 = DeviceRepository.Get(device.ID);
            Assert.AreEqual("Test2", device3.Name);
            Assert.AreEqual("Status", device3.Status);
            Assert.AreEqual("{}", device3.Data);
            Assert.IsNull(device3.Network);
            Assert.IsNull(device3.NetworkID);

            // test update relationship
            var deviceClass2 = new DeviceClass("D2", "V2");
            DeviceClassRepository.Save(deviceClass2);
            device.DeviceClass = deviceClass2;
            DeviceRepository.Save(device);
            var device4 = DeviceRepository.Get(device.ID);
            Assert.AreEqual(deviceClass2.ID, device4.DeviceClassID);
            Assert.IsNotNull(device4.DeviceClass);

            // test Delete
            DeviceRepository.Delete(device.ID);
            var device5 = DeviceRepository.Get(device.ID);
            Assert.IsNull(device5);
        }

        [Test]
        public void DeviceNotificationTest()
        {
            var network = new Network("N1");
            NetworkRepository.Save(network);

            var deviceClass = new DeviceClass("D1", "V1");
            DeviceClassRepository.Save(deviceClass);

            var device = new Device(Guid.NewGuid(), "key", "Test", network, deviceClass);
            DeviceRepository.Save(device);

            var notification = new DeviceNotification("Test", device);
            DeviceNotificationRepository.Save(notification);

            // test GetByDevice
            var notifications = DeviceNotificationRepository.GetByDevice(device.ID, null, null);
            Assert.Greater(notifications.Count, 0);

            // test Get(id)
            var notification1 = DeviceNotificationRepository.Get(notification.ID);
            Assert.IsNotNull(notification1);
            Assert.AreEqual("Test", notification1.Notification);
            Assert.AreEqual(device.ID, notification1.DeviceID);

            // test Save
            notification.Notification = "Test2";
            notification.Parameters = "{}";
            DeviceNotificationRepository.Save(notification);
            var notification2 = DeviceNotificationRepository.Get(notification.ID);
            Assert.AreEqual("Test2", notification2.Notification);
            Assert.AreEqual("{}", notification2.Parameters);

            // test Delete
            DeviceNotificationRepository.Delete(notification.ID);
            var notification3 = DeviceNotificationRepository.Get(notification.ID);
            Assert.IsNull(notification3);
        }

        [Test]
        public void DeviceCommandTest()
        {
            var network = new Network("N1");
            NetworkRepository.Save(network);

            var deviceClass = new DeviceClass("D1", "V1");
            DeviceClassRepository.Save(deviceClass);

            var device = new Device(Guid.NewGuid(), "key", "Test", network, deviceClass);
            DeviceRepository.Save(device);

            var command = new DeviceCommand("Test", device);
            DeviceCommandRepository.Save(command);

            // test GetByDevice
            var commands = DeviceCommandRepository.GetByDevice(device.ID, null, null);
            Assert.Greater(commands.Count, 0);

            // test Get(id)
            var command1 = DeviceCommandRepository.Get(command.ID);
            Assert.IsNotNull(command1);
            Assert.AreEqual("Test", command1.Command);
            Assert.AreEqual(device.ID, command1.DeviceID);

            // test Save
            command.Command = "Test2";
            command.Parameters = "{}";
            command.Status = "OK";
            command.Result = "Success";
            DeviceCommandRepository.Save(command);
            var command2 = DeviceCommandRepository.Get(command.ID);
            Assert.AreEqual("Test2", command2.Command);
            Assert.AreEqual("{}", command2.Parameters);
            Assert.AreEqual("OK", command2.Status);
            Assert.AreEqual("Success", command2.Result);

            // test Delete
            DeviceCommandRepository.Delete(command.ID);
            var command3 = DeviceCommandRepository.Get(command.ID);
            Assert.IsNull(command3);
        }

        [Test]
        public void DeviceEquipmentTest()
        {
            var network = new Network("N1");
            NetworkRepository.Save(network);

            var deviceClass = new DeviceClass("D1", "V1");
            DeviceClassRepository.Save(deviceClass);

            var device = new Device(Guid.NewGuid(), "key", "Test", network, deviceClass);
            DeviceRepository.Save(device);

            var equipment = new DeviceEquipment("Test", DateTime.UtcNow, device);
            DeviceEquipmentRepository.Save(equipment);

            // test GetByDevice
            var equipments = DeviceEquipmentRepository.GetByDevice(device.ID);
            Assert.Greater(equipments.Count, 0);

            // test GetByDeviceAndCode
            var equipment0 = DeviceEquipmentRepository.GetByDeviceAndCode(device.ID, "Test");
            Assert.IsNotNull(equipment0);

            // test Get(id)
            var equipment1 = DeviceEquipmentRepository.Get(equipment.ID);
            Assert.IsNotNull(equipment1);
            Assert.AreEqual("Test", equipment1.Code);
            Assert.AreEqual(device.ID, equipment1.DeviceID);

            // test Save
            equipment.Code = "Test2";
            equipment.Parameters = "{}";
            DeviceEquipmentRepository.Save(equipment);
            var equipment2 = DeviceEquipmentRepository.Get(equipment.ID);
            Assert.AreEqual("Test2", equipment2.Code);
            Assert.AreEqual("{}", equipment2.Parameters);

            // test Delete
            DeviceEquipmentRepository.Delete(equipment.ID);
            var equipment3 = DeviceEquipmentRepository.Get(equipment.ID);
            Assert.IsNull(equipment3);
        }

        public IUserRepository UserRepository
        {
            get { return new UserRepository(); }
        }

        public IUserNetworkRepository UserNetworkRepository
        {
            get { return new UserNetworkRepository(); }
        }

        public INetworkRepository NetworkRepository
        {
            get { return new NetworkRepository(); }
        }

        public IDeviceClassRepository DeviceClassRepository
        {
            get { return new DeviceClassRepository(); }
        }

        public IEquipmentRepository EquipmentRepository
        {
            get { return new EquipmentRepository(); }
        }

        public IDeviceRepository DeviceRepository
        {
            get { return new DeviceRepository(); }
        }

        public IDeviceNotificationRepository DeviceNotificationRepository
        {
            get { return new DeviceNotificationRepository(); }
        }

        public IDeviceCommandRepository DeviceCommandRepository
        {
            get { return new DeviceCommandRepository(); }
        }

        public IDeviceEquipmentRepository DeviceEquipmentRepository
        {
            get { return new DeviceEquipmentRepository(); }
        }
    }
}
