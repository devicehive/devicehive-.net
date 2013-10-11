using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeviceHive.Data;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;
using NUnit.Framework;

namespace DeviceHive.Test.DataTest
{
    public abstract class BaseDataTest
    {
        private Stack<Action> _tearDownActions = new Stack<Action>();

        [SetUp]
        protected virtual void SetUp()
        {
        }

        [TearDown]
        protected virtual void TearDown()
        {
            while (_tearDownActions.Count > 0)
            {
                try
                {
                    _tearDownActions.Pop()();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        protected DataContext DataContext { get; set; }

        protected void RegisterTearDown(Action action)
        {
            _tearDownActions.Push(action);
        }

        [Test]
        public void User()
        {
            var user = new User("Test", "TestPass", (int)UserRole.Administrator, (int)UserStatus.Active);
            DataContext.User.Save(user);
            RegisterTearDown(() => DataContext.User.Delete(user.ID));

            // test GetAll
            var users = DataContext.User.GetAll();
            Assert.Greater(users.Count, 0);

            // test Get(id)
            var user1 = DataContext.User.Get(user.ID);
            Assert.IsNotNull(user1);
            Assert.AreEqual("Test", user1.Login);
            Assert.IsTrue(user1.IsValidPassword("TestPass"));
            Assert.IsFalse(user1.IsValidPassword("TestPass2"));
            Assert.AreEqual((int)UserRole.Administrator, user1.Role);
            Assert.AreEqual((int)UserStatus.Active, user1.Status);

            // test Get(name)
            var user2 = DataContext.User.Get("Test");
            Assert.IsNotNull(user2);

            // test Save
            user.Login = "Test2";
            user.SetPassword("TestPass2");
            user.Role = (int)UserRole.Client;
            user.Status = (int)UserStatus.Disabled;
            user.LastLogin = DateTime.UtcNow;
            user.LoginAttempts = 1;
            DataContext.User.Save(user);
            var user3 = DataContext.User.Get(user.ID);
            Assert.AreEqual("Test2", user3.Login);
            Assert.IsTrue(user3.IsValidPassword("TestPass2"));
            Assert.AreEqual((int)UserRole.Client, user3.Role);
            Assert.AreEqual((int)UserStatus.Disabled, user3.Status);
            Assert.IsNotNull(user3.LastLogin);
            Assert.AreEqual(1, user3.LoginAttempts);

            // test Delete
            DataContext.User.Delete(user.ID);
            var user4 = DataContext.User.Get(user.ID);
            Assert.IsNull(user4);
        }

        [Test]
        public void AccessKey()
        {
            var user = new User("Test", "TestPass", (int)UserRole.Administrator, (int)UserStatus.Active);
            DataContext.User.Save(user);
            RegisterTearDown(() => DataContext.User.Delete(user.ID));

            var accessKey = new AccessKey(user.ID, "Test");
            accessKey.Permissions.Add(new AccessKeyPermission { Subnets = new[] { "127.0.0.1" } });
            accessKey.Permissions.Add(new AccessKeyPermission { Subnets = new[] { "127.0.0.2" } });
            DataContext.AccessKey.Save(accessKey);
            RegisterTearDown(() => DataContext.AccessKey.Delete(accessKey.ID));

            // test GetByUser
            var accessKeys = DataContext.AccessKey.GetByUser(user.ID);
            Assert.AreEqual(1, accessKeys.Count);
            Assert.AreEqual(accessKey.ID, accessKeys[0].ID);
            Assert.AreEqual(user.ID, accessKeys[0].UserID);

            // test Get(id)
            var accessKey1 = DataContext.AccessKey.Get(accessKey.ID);
            Assert.IsNotNull(accessKey1);
            Assert.AreEqual("Test", accessKey1.Label);
            Assert.AreEqual(accessKey.Key, accessKey1.Key);
            Assert.IsNotNull(accessKey1.Permissions);
            Assert.AreEqual(2, accessKey1.Permissions.Count);
            Assert.AreEqual(new[] { "127.0.0.1" }, accessKey1.Permissions[0].Subnets);
            Assert.AreEqual(new[] { "127.0.0.2" }, accessKey1.Permissions[1].Subnets);

            // test Save
            accessKey.Label = "Test2";
            accessKey.GenerateKey();
            accessKey.ExpirationDate = DateTime.UtcNow;
            accessKey.Permissions.RemoveAt(1);
            accessKey.Permissions.Add(new AccessKeyPermission {
                Subnets = new[] { "127.0.0.3" },
                Domains = new[] { "www.example.com" },
                Networks = new[] { 1, 2, 3 }});
            DataContext.AccessKey.Save(accessKey);
            var accessKey2 = DataContext.AccessKey.Get(accessKey.ID);
            Assert.AreEqual("Test2", accessKey2.Label);
            Assert.AreEqual(accessKey.Key, accessKey2.Key);
            Assert.IsNotNull(accessKey2.ExpirationDate);
            Assert.IsNotNull(accessKey2.Permissions);
            Assert.AreEqual(2, accessKey2.Permissions.Count);
            Assert.AreEqual(new[] { "127.0.0.1" }, accessKey2.Permissions[0].Subnets);
            Assert.AreEqual(new[] { "127.0.0.3" }, accessKey2.Permissions[1].Subnets);
            Assert.AreEqual(new[] { "www.example.com" }, accessKey2.Permissions[1].Domains);
            Assert.AreEqual(new[] { 1, 2, 3 }, accessKey2.Permissions[1].Networks);

            // test Delete
            DataContext.AccessKey.Delete(accessKey.ID);
            var accessKey3 = DataContext.AccessKey.Get(accessKey.ID);
            Assert.IsNull(accessKey3);
        }

        [Test]
        public void UserNetwork()
        {
            var user = new User("Test", "TestPass", (int)UserRole.Administrator, (int)UserStatus.Active);
            DataContext.User.Save(user);
            RegisterTearDown(() => DataContext.User.Delete(user.ID));

            var network = new Network("Test");
            DataContext.Network.Save(network);
            RegisterTearDown(() => DataContext.Network.Delete(network.ID));

            var userNetwork = new UserNetwork(user, network);
            DataContext.UserNetwork.Save(userNetwork);
            RegisterTearDown(() => DataContext.UserNetwork.Delete(userNetwork.ID));

            // test GetByUser
            var userNetworks1 = DataContext.UserNetwork.GetByUser(user.ID);
            Assert.Greater(userNetworks1.Count, 0);

            // test GetByNetwork
            var userNetworks2 = DataContext.UserNetwork.GetByNetwork(network.ID);
            Assert.Greater(userNetworks2.Count, 0);

            // test Get(id)
            var userNetwork1 = DataContext.UserNetwork.Get(userNetwork.ID);
            Assert.IsNotNull(userNetwork1);
            Assert.AreEqual(user.ID, userNetwork1.UserID);
            Assert.AreEqual(network.ID, userNetwork1.NetworkID);
            Assert.IsNotNull(userNetwork1.User);
            Assert.IsNotNull(userNetwork1.Network);

            // test Get(userId, networkId)
            var userNetwork2 = DataContext.UserNetwork.Get(user.ID, network.ID);
            Assert.IsNotNull(userNetwork2);

            // test Delete
            DataContext.UserNetwork.Delete(userNetwork.ID);
            var userNetwork3 = DataContext.UserNetwork.Get(userNetwork.ID);
            Assert.IsNull(userNetwork3);
        }

        [Test]
        public void Network()
        {
            var network = new Network("Test") { Key = "Key" };
            DataContext.Network.Save(network);
            RegisterTearDown(() => DataContext.Network.Delete(network.ID));

            // test GetAll
            var networks = DataContext.Network.GetAll();
            Assert.Greater(networks.Count, 0);

            // test Get(id)
            var network1 = DataContext.Network.Get(network.ID);
            Assert.IsNotNull(network1);
            Assert.AreEqual("Test", network1.Name);

            // test Get(name)
            var network2 = DataContext.Network.Get("Test");
            Assert.IsNotNull(network2);

            // test Save
            network.Name = "Test2";
            network.Description = "Desc";
            DataContext.Network.Save(network);
            var network4 = DataContext.Network.Get(network.ID);
            Assert.AreEqual("Test2", network4.Name);
            Assert.AreEqual("Desc", network4.Description);

            // test Delete
            DataContext.Network.Delete(network.ID);
            var network5 = DataContext.Network.Get(network.ID);
            Assert.IsNull(network5);
        }

        [Test]
        public void DeviceClass()
        {
            var deviceClass = new DeviceClass("Test", "V1");
            deviceClass.Equipment.Add(new Equipment("name1", "code1", "type1"));
            deviceClass.Equipment.Add(new Equipment("name2", "code2", "type2"));
            DataContext.DeviceClass.Save(deviceClass);
            RegisterTearDown(() => DataContext.DeviceClass.Delete(deviceClass.ID));

            // test GetAll
            var deviceClasses = DataContext.DeviceClass.GetAll();
            Assert.Greater(deviceClasses.Count, 0);

            // test Get(id)
            var deviceClass1 = DataContext.DeviceClass.Get(deviceClass.ID);
            Assert.IsNotNull(deviceClass1);
            Assert.AreEqual("Test", deviceClass1.Name);
            Assert.IsNotNull(deviceClass1.Equipment);
            Assert.AreEqual(2, deviceClass1.Equipment.Count);
            Assert.AreEqual("name1", deviceClass1.Equipment[0].Name);
            Assert.AreEqual("name2", deviceClass1.Equipment[1].Name);

            // test Get(name, version)
            var deviceClass2 = DataContext.DeviceClass.Get("Test", "V1");
            Assert.IsNotNull(deviceClass2);

            // test Save
            deviceClass.Name = "Test2";
            deviceClass.Version = "V2";
            deviceClass.IsPermanent = true;
            deviceClass.OfflineTimeout = 10;
            deviceClass.Data = "{ }";
            deviceClass.Equipment.RemoveAt(1);
            deviceClass.Equipment.Add(new Equipment("name3", "code3", "type3"));
            DataContext.DeviceClass.Save(deviceClass);
            var deviceClass3 = DataContext.DeviceClass.Get(deviceClass.ID);
            Assert.AreEqual("Test2", deviceClass3.Name);
            Assert.AreEqual("V2", deviceClass3.Version);
            Assert.AreEqual(true, deviceClass3.IsPermanent);
            Assert.AreEqual(10, deviceClass3.OfflineTimeout);
            Assert.AreEqual("{ }", deviceClass3.Data);
            Assert.AreEqual(2, deviceClass3.Equipment.Count);
            Assert.AreEqual("name1", deviceClass3.Equipment[0].Name);
            Assert.AreEqual("name3", deviceClass3.Equipment[1].Name);

            // test Delete
            DataContext.DeviceClass.Delete(deviceClass.ID);
            var deviceClass4 = DataContext.DeviceClass.Get(deviceClass.ID);
            Assert.IsNull(deviceClass4);
        }

        [Test]
        public void Device()
        {
            var network = new Network("N1");
            DataContext.Network.Save(network);
            RegisterTearDown(() => DataContext.Network.Delete(network.ID));

            var deviceClass = new DeviceClass("D1", "V1");
            DataContext.DeviceClass.Save(deviceClass);
            RegisterTearDown(() => DataContext.DeviceClass.Delete(deviceClass.ID));

            var device = new Device(Guid.NewGuid(), "key", "Test", network, deviceClass);
            DataContext.Device.Save(device);
            RegisterTearDown(() => DataContext.Device.Delete(device.ID));

            // test GetByNetwork
            var devices = DataContext.Device.GetByNetwork(network.ID);
            Assert.Greater(devices.Count, 0);

            // test Get(id)
            var device1 = DataContext.Device.Get(device.ID);
            Assert.IsNotNull(device1);
            Assert.AreEqual(device.GUID, device1.GUID);
            Assert.AreEqual("Test", device1.Name);
            Assert.AreEqual(network.ID, device1.NetworkID);
            Assert.AreEqual(deviceClass.ID, device1.DeviceClassID);
            Assert.IsNotNull(device1.Network);
            Assert.IsNotNull(device1.DeviceClass);

            // test Get(guid)
            var device2 = DataContext.Device.Get(device.GUID);
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
            device.Data = "{ }";
            device.Network = null;
            device.NetworkID = null;
            DataContext.Device.Save(device);
            var device3 = DataContext.Device.Get(device.ID);
            Assert.AreEqual("Test2", device3.Name);
            Assert.AreEqual("Status", device3.Status);
            Assert.AreEqual("{ }", device3.Data);
            Assert.IsNull(device3.Network);
            Assert.IsNull(device3.NetworkID);

            // test update relationship
            var deviceClass2 = new DeviceClass("D2", "V2");
            DataContext.DeviceClass.Save(deviceClass2);
            RegisterTearDown(() => DataContext.DeviceClass.Delete(deviceClass2.ID));
            device.DeviceClass = deviceClass2;
            DataContext.Device.Save(device);
            var device4 = DataContext.Device.Get(device.ID);
            Assert.AreEqual(deviceClass2.ID, device4.DeviceClassID);
            Assert.IsNotNull(device4.DeviceClass);

            // test Delete
            DataContext.Device.Delete(device.ID);
            var device5 = DataContext.Device.Get(device.ID);
            Assert.IsNull(device5);
        }

        [Test]
        public void DeviceNotification()
        {
            var network = new Network("N1");
            DataContext.Network.Save(network);
            RegisterTearDown(() => DataContext.Network.Delete(network.ID));

            var deviceClass = new DeviceClass("D1", "V1");
            DataContext.DeviceClass.Save(deviceClass);
            RegisterTearDown(() => DataContext.DeviceClass.Delete(deviceClass.ID));

            var device = new Device(Guid.NewGuid(), "key", "Test", network, deviceClass);
            DataContext.Device.Save(device);
            RegisterTearDown(() => DataContext.Device.Delete(device.ID));

            var notification = new DeviceNotification("Test", device);
            DataContext.DeviceNotification.Save(notification);
            RegisterTearDown(() => DataContext.DeviceNotification.Delete(notification.ID));

            // test GetByDevice
            var notifications = DataContext.DeviceNotification.GetByDevice(device.ID);
            Assert.Greater(notifications.Count, 0);

            // test Get(id)
            var notification1 = DataContext.DeviceNotification.Get(notification.ID);
            Assert.IsNotNull(notification1);
            Assert.AreEqual("Test", notification1.Notification);
            Assert.AreEqual(device.ID, notification1.DeviceID);

            // test Save
            notification.Notification = "Test2";
            notification.Parameters = "{ }";
            DataContext.DeviceNotification.Save(notification);
            var notification2 = DataContext.DeviceNotification.Get(notification.ID);
            Assert.AreEqual("Test2", notification2.Notification);
            Assert.AreEqual("{ }", notification2.Parameters);

            // test Delete
            DataContext.DeviceNotification.Delete(notification.ID);
            var notification3 = DataContext.DeviceNotification.Get(notification.ID);
            Assert.IsNull(notification3);
        }

        [Test]
        public void DeviceCommand()
        {
            var network = new Network("N1");
            DataContext.Network.Save(network);
            RegisterTearDown(() => DataContext.Network.Delete(network.ID));

            var deviceClass = new DeviceClass("D1", "V1");
            DataContext.DeviceClass.Save(deviceClass);
            RegisterTearDown(() => DataContext.DeviceClass.Delete(deviceClass.ID));

            var device = new Device(Guid.NewGuid(), "key", "Test", network, deviceClass);
            DataContext.Device.Save(device);
            RegisterTearDown(() => DataContext.Device.Delete(device.ID));

            var command = new DeviceCommand("Test", device);
            DataContext.DeviceCommand.Save(command);
            RegisterTearDown(() => DataContext.DeviceCommand.Delete(command.ID));

            // test GetByDevice
            var commands = DataContext.DeviceCommand.GetByDevice(device.ID);
            Assert.Greater(commands.Count, 0);

            // test Get(id)
            var command1 = DataContext.DeviceCommand.Get(command.ID);
            Assert.IsNotNull(command1);
            Assert.AreEqual("Test", command1.Command);
            Assert.AreEqual(device.ID, command1.DeviceID);

            // test Save
            command.Command = "Test2";
            command.Parameters = "{ }";
            command.Status = "OK";
            command.Result = "\"Success\"";
            command.UserID = 1;
            DataContext.DeviceCommand.Save(command);
            var command2 = DataContext.DeviceCommand.Get(command.ID);
            Assert.AreEqual("Test2", command2.Command);
            Assert.AreEqual("{ }", command2.Parameters);
            Assert.AreEqual("OK", command2.Status);
            Assert.AreEqual("\"Success\"", command2.Result);
            Assert.AreEqual(1, command2.UserID);

            // test Delete
            DataContext.DeviceCommand.Delete(command.ID);
            var command3 = DataContext.DeviceCommand.Get(command.ID);
            Assert.IsNull(command3);
        }

        [Test]
        public void DeviceEquipment()
        {
            var network = new Network("N1");
            DataContext.Network.Save(network);
            RegisterTearDown(() => DataContext.Network.Delete(network.ID));

            var deviceClass = new DeviceClass("D1", "V1");
            DataContext.DeviceClass.Save(deviceClass);
            RegisterTearDown(() => DataContext.DeviceClass.Delete(deviceClass.ID));

            var device = new Device(Guid.NewGuid(), "key", "Test", network, deviceClass);
            DataContext.Device.Save(device);
            RegisterTearDown(() => DataContext.Device.Delete(device.ID));

            var equipment = new DeviceEquipment("Test", DateTime.UtcNow, device);
            DataContext.DeviceEquipment.Save(equipment);
            RegisterTearDown(() => DataContext.DeviceEquipment.Delete(equipment.ID));

            // test GetByDevice
            var equipments = DataContext.DeviceEquipment.GetByDevice(device.ID);
            Assert.Greater(equipments.Count, 0);

            // test GetByDeviceAndCode
            var equipment0 = DataContext.DeviceEquipment.GetByDeviceAndCode(device.ID, "Test");
            Assert.IsNotNull(equipment0);

            // test Get(id)
            var equipment1 = DataContext.DeviceEquipment.Get(equipment.ID);
            Assert.IsNotNull(equipment1);
            Assert.AreEqual("Test", equipment1.Code);
            Assert.AreEqual(device.ID, equipment1.DeviceID);

            // test Save
            equipment.Code = "Test2";
            equipment.Parameters = "{ }";
            DataContext.DeviceEquipment.Save(equipment);
            var equipment2 = DataContext.DeviceEquipment.Get(equipment.ID);
            Assert.AreEqual("Test2", equipment2.Code);
            Assert.AreEqual("{ }", equipment2.Parameters);

            // test Delete
            DataContext.DeviceEquipment.Delete(equipment.ID);
            var equipment3 = DataContext.DeviceEquipment.Get(equipment.ID);
            Assert.IsNull(equipment3);
        }

        [Test]
        public void OAuthClient()
        {
            var client = new OAuthClient("Test", "test.com", "http://test.com/oauth2", "test_client");
            DataContext.OAuthClient.Save(client);
            RegisterTearDown(() => DataContext.OAuthClient.Delete(client.ID));

            // test GetAll
            var clients = DataContext.OAuthClient.GetAll();
            Assert.Greater(clients.Count, 0);

            // test Get(id)
            var client1 = DataContext.OAuthClient.Get(client.ID);
            Assert.IsNotNull(client1);
            Assert.AreEqual("Test", client1.Name);
            Assert.AreEqual("test.com", client1.Domain);
            Assert.AreEqual("http://test.com/oauth2", client1.RedirectUri);
            Assert.AreEqual("test_client", client1.OAuthID);
            Assert.IsNotNull(client1.OAuthSecret);

            // test Get(oauthId)
            var client2 = DataContext.OAuthClient.Get("test_client");
            Assert.IsNotNull(client2);

            // test Save
            client.Name = "Test2";
            client.Domain = "test2.com";
            client.Subnet = "127.0.0.1";
            client.RedirectUri = "http://test.com/oauth/2";
            client.OAuthID = "test_client2";
            DataContext.OAuthClient.Save(client);
            var client3 = DataContext.OAuthClient.Get(client.ID);
            Assert.AreEqual("Test2", client3.Name);
            Assert.AreEqual("test2.com", client3.Domain);
            Assert.AreEqual("127.0.0.1", client3.Subnet);
            Assert.AreEqual("http://test.com/oauth/2", client3.RedirectUri);
            Assert.AreEqual("test_client2", client3.OAuthID);

            // test Delete
            DataContext.OAuthClient.Delete(client.ID);
            var client4 = DataContext.OAuthClient.Get(client.ID);
            Assert.IsNull(client4);
        }

        [Test]
        public void OAuthGrant()
        {
            var user = new User("Test", "pass", 0, 0);
            DataContext.User.Save(user);
            RegisterTearDown(() => DataContext.User.Delete(user.ID));

            var accessKey = new AccessKey(user.ID, "test");
            DataContext.AccessKey.Save(accessKey);
            RegisterTearDown(() => DataContext.AccessKey.Delete(accessKey.ID));

            var client = new OAuthClient("Test", "test.com", "http://test.com/oauth2", "test_client");
            DataContext.OAuthClient.Save(client);
            RegisterTearDown(() => DataContext.OAuthClient.Delete(client.ID));

            var grant = new OAuthGrant(client, user.ID, accessKey, 0, "scope");
            grant.AuthCode = Guid.NewGuid();
            DataContext.OAuthGrant.Save(grant);
            RegisterTearDown(() => DataContext.OAuthGrant.Delete(grant.ID));

            // test GetByUser
            var grants = DataContext.OAuthGrant.GetByUser(user.ID);
            Assert.Greater(grants.Count, 0);

            // test Get(id)
            var grant1 = DataContext.OAuthGrant.Get(grant.ID);
            Assert.IsNotNull(grant1);
            Assert.Less(Math.Abs(DateTime.UtcNow.Subtract(grant1.Timestamp).TotalMinutes), 10);
            Assert.AreEqual(0, grant1.Type);
            Assert.AreEqual("scope", grant1.Scope);
            Assert.AreEqual(client.ID, grant1.ClientID);
            Assert.IsNotNull(grant1.Client);
            Assert.AreEqual(user.ID, grant1.UserID);
            Assert.AreEqual(accessKey.ID, grant1.AccessKeyID);
            Assert.IsNotNull(grant1.AccessKey);

            // test Get(authCode)
            var grant2 = DataContext.OAuthGrant.Get(grant.AuthCode.Value);
            Assert.IsNotNull(grant2);
            Assert.AreEqual(0, grant2.Type);
            Assert.AreEqual("scope", grant2.Scope);
            Assert.AreEqual(user.ID, grant2.UserID);
            Assert.AreEqual(client.ID, grant2.ClientID);
            Assert.IsNotNull(grant2.Client);
            Assert.AreEqual(accessKey.ID, grant2.AccessKeyID);
            Assert.IsNotNull(grant2.AccessKey);

            // test Save
            grant.AuthCode = Guid.NewGuid();
            grant.Type = 1;
            grant.AccessType = 1;
            grant.RedirectUri = "http://test.com/oauth";
            grant.Scope = "scope scope2";
            grant.NetworkList = "5,10";
            DataContext.OAuthGrant.Save(grant);
            var grant3 = DataContext.OAuthGrant.Get(grant.ID);
            Assert.AreEqual(grant.AuthCode, grant3.AuthCode);
            Assert.AreEqual(1, grant3.Type);
            Assert.AreEqual(1, grant3.AccessType);
            Assert.AreEqual("http://test.com/oauth", grant3.RedirectUri);
            Assert.AreEqual("scope scope2", grant3.Scope);
            Assert.AreEqual("5,10", grant3.NetworkList);
            Assert.AreEqual(user.ID, grant3.UserID);
            Assert.AreEqual(client.ID, grant3.ClientID);
            Assert.IsNotNull(grant3.Client);
            Assert.AreEqual(accessKey.ID, grant3.AccessKeyID);
            Assert.IsNotNull(grant3.AccessKey);

            // test update relationship
            var client2 = new OAuthClient("Test2", "test2.com", "http://test.com/oauth/2", "test_client2");
            DataContext.OAuthClient.Save(client2);
            RegisterTearDown(() => DataContext.OAuthClient.Delete(client2.ID));
            grant.Client = client2;
            DataContext.OAuthGrant.Save(grant);
            var grant4 = DataContext.OAuthGrant.Get(grant.ID);
            Assert.AreEqual(client2.ID, grant4.ClientID);
            Assert.IsNotNull(grant4.Client);

            // test Delete
            DataContext.OAuthClient.Delete(grant.ID);
            var grant5 = DataContext.OAuthClient.Get(grant.ID);
            Assert.IsNull(grant5);
        }

        [Test]
        public void Timestamp()
        {
            var timestamp = DataContext.Timestamp.GetCurrentTimestamp();
            Assert.Less(Math.Abs(DateTime.UtcNow.Subtract(timestamp).TotalMinutes), 10);
        }
    }
}
