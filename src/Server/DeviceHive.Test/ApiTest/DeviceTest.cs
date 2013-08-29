using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DeviceHive.Test.ApiTest
{
    [TestFixture]
    public class DeviceTest : ResourceTest
    {
        private static readonly string ID = "a97266f4-6e8a-4008-8242-022b49ea484f";
        private int? NetworkID { get; set; }
        private int? DeviceClassID { get; set; }

        public DeviceTest()
            : base("/device")
        {
        }

        protected override void OnCreateDependencies()
        {
            var networkResponse = Client.Post("/network", new { name = "_ut_n" }, auth: Admin);
            Assert.That(networkResponse.Status, Is.EqualTo(ExpectedCreatedStatus));
            NetworkID = (int)networkResponse.Json["id"];
            RegisterForDeletion("/network/" + NetworkID);

            var deviceClassResponse = Client.Post("/device/class", new { name = "_ut_dc", version = "1",
                equipment = new[] { new { name = "_ut_eq", code = "_ut_eq", type = "_ut_eq" } } }, auth: Admin);
            Assert.That(deviceClassResponse.Status, Is.EqualTo(ExpectedCreatedStatus));
            DeviceClassID = (int)deviceClassResponse.Json["id"];
            RegisterForDeletion("/device/class/" + DeviceClassID);
        }

        [Test]
        public void GetAll()
        {
            // create new device
            Update(ID, new { key = "key", name = "_ut", network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } }, auth: Admin);
            RegisterForDeletion(ResourceUri + "/" + ID);

            // create another device
            var anotherDeviceId = Guid.NewGuid().ToString();
            Update(anotherDeviceId, new { key = "key", name = "_ut2", network = new { name = "_ut_n_a" }, deviceClass = new { name = "_ut_dc_a", version = "1" } }, auth: Admin);
            var anotherDevice = Get(anotherDeviceId, auth: Admin);
            RegisterForDeletion("/network/" + (int)anotherDevice["network"]["id"]);
            RegisterForDeletion("/device/class/" + (int)anotherDevice["deviceClass"]["id"]);
            RegisterForDeletion(ResourceUri + "/" + anotherDeviceId);

            // admin: get device by name
            var devices = List(new Dictionary<string, string> { { "name", "_ut" } }, auth: Admin);
            Expect(devices.Count, Is.EqualTo(1));
            Expect(devices[0], Matches(new { id = ID }));

            // admin: get device by network
            devices = List(new Dictionary<string, string> { { "networkId", NetworkID.ToString() } }, auth: Admin);
            Expect(devices.Count, Is.EqualTo(1));
            Expect(devices[0], Matches(new { id = ID }));

            // admin: get device by device class
            devices = List(new Dictionary<string, string> { { "deviceClassId", DeviceClassID.ToString() } }, auth: Admin);
            Expect(devices.Count, Is.EqualTo(1));
            Expect(devices[0], Matches(new { id = ID }));

            // user: get all devices
            var user = CreateUser(1, NetworkID);
            devices = List(auth: user);
            Expect(devices.Count, Is.EqualTo(1));
            Expect(devices[0], Matches(new { id = ID }));

            // accesskey: get all devices
            var accessKey = CreateAccessKey(user, "GetDevice");
            devices = List(auth: accessKey);
            Expect(devices.Count, Is.EqualTo(1));
            Expect(devices[0], Matches(new { id = ID }));

            // accesskey: get all devices with no access
            accessKey = CreateAccessKey(user, "GetDevice", new[] { 0 });
            Expect(List(auth: accessKey).Count, Is.EqualTo(0));
            accessKey = CreateAccessKey(user, "GetDevice", null, new[] { Guid.NewGuid().ToString() });
            Expect(List(auth: accessKey).Count, Is.EqualTo(0));
        }

        [Test]
        public void Get()
        {
            // create new device
            Update(ID, new { key = "key", name = "_ut", network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } }, auth: Admin);
            RegisterForDeletion(ResourceUri + "/" + ID);

            // users can receive device resource when associated with the network
            var user1 = CreateUser(1); // create a client user
            var user2 = CreateUser(1, NetworkID); // create a client user with access to network
            Expect(() => Get(ID, auth: user1), FailsWith(404)); // should fail with 404
            Get(ID, auth: user2); // should succeed

            // access keys can receive device resource when have necessary permissions
            var accessKey1 = CreateAccessKey(user1, "GetDevice");
            var accessKey2 = CreateAccessKey(user2, "GetDevice", networks: new[] { 0 });
            var accessKey3 = CreateAccessKey(user2, "GetDevice", devices: new[] { Guid.NewGuid().ToString() });
            var accessKey4 = CreateAccessKey(user2, "GetDevice");
            Expect(() => Get(ID, auth: accessKey1), FailsWith(404)); // should fail with 404
            Expect(() => Get(ID, auth: accessKey2), FailsWith(404)); // should fail with 404
            Expect(() => Get(ID, auth: accessKey3), FailsWith(404)); // should fail with 404
            Get(ID, auth: accessKey4); // should succeed

            // devices can receive device resource when specify a valid key
            Expect(() => Get(ID, auth: Device(ID, "wrong_key")), FailsWith(401)); // should fail with 401
            var device = Get(ID, auth: Device(ID, "key")); // should succeed
        }

        [Test]
        public void Create()
        {
            // create new device
            Update(ID, new { key = "key", name = "_ut", network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } });
            RegisterForDeletion(ResourceUri + "/" + ID);

            // expect valid server response
            Expect(Get(ID, auth: Admin), Matches(new { id = ID, name = "_ut", network = new { name = "_ut_n" }, deviceClass = new {
                name = "_ut_dc", version = "1", equipment = new[] { new { name = "_ut_eq", code = "_ut_eq", type = "_ut_eq" } } } }));
            
            // verify device-add notification
            var notificationResponse = Client.Get("/device/" + ID + "/notification", auth: Admin);
            Expect(notificationResponse.Json.Count(), Is.EqualTo(1));
            Expect(notificationResponse.Json[0], Matches(new { notification = "$device-add", parameters =
                new { id = ID, name = "_ut", network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } } }));
        }

        [Test]
        public void Create_Client()
        {
            // create new device
            var user1 = CreateUser(1); // create a client user
            var user2 = CreateUser(1, NetworkID); // create a client user with access to network

            // device creation for user1 should fail, for user2 should succeed
            RegisterForDeletion(ResourceUri + "/" + ID);
            Expect(() => { Update(ID, new { key = "key", name = "_ut", network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } }, auth: user1); return false; }, FailsWith(403));
            Update(ID, new { key = "key", name = "_ut", network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } }, auth: user2);

            // expect valid server response
            Expect(Get(ID, auth: Admin), Matches(new { id = ID, name = "_ut", network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } }));
        }

        [Test]
        public void Create_NetworkKey()
        {
            // set a key to the network
            Client.Put("/network/" + NetworkID, new { key = "network_key" }, auth: Admin);

            // referencing network without key is not allowed
            RegisterForDeletion(ResourceUri + "/" + ID);
            Expect(() => { Update(ID, new { key = "key", name = "_ut", network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } }); return false; }, FailsWith(403));

            // when key is passed, registration should succeed
            Update(ID, new { key = "key", name = "_ut", network = new { name = "_ut_n", key = "network_key" }, deviceClass = new { name = "_ut_dc", version = "1" } });
            Expect(Get(ID, auth: Admin), Matches(new { id = ID, name = "_ut", network = new { name = "_ut_n", key = "network_key" }, deviceClass = new { name = "_ut_dc", version = "1" } }));

            // verify that network key is not exposed to devices
            var device = Get(ID, auth: Device(ID, "key")); // should succeed
            Expect(device["network"]["key"], Is.Null); // verify that network does not include key
        }

        [Test]
        public void Create_RefCreate()
        {
            // both network and device class should auto-create
            Update(ID, new { key = "key", name = "_ut", network = new { name = "_ut_n_a" }, deviceClass = new {
                name = "_ut_dc_a", version = "1", equipment = new[] { new { name = "_ut_eq", code = "_ut_eq", type = "_ut_eq" } } } });

            var get = Get(ID, auth: Admin);
            RegisterForDeletion("/network/" + (int)get["network"]["id"]);
            RegisterForDeletion("/device/class/" + (int)get["deviceClass"]["id"]);
            RegisterForDeletion(ResourceUri + "/" + ID);

            Expect(get, Matches(new { id = ID, name = "_ut", network = new { name = "_ut_n_a" }, deviceClass = new {
                name = "_ut_dc_a", version = "1", equipment = new[] { new { name = "_ut_eq", code = "_ut_eq", type = "_ut_eq" } } } }));
        }

        [Test]
        public void Create_LegacyEquipment()
        {
            // eqipment should auto-create using compatibility code (version 1.2)
            Update(ID, new { key = "key", name = "_ut", network = new { name = "_ut_n_a" }, deviceClass = new { name = "_ut_dc_a", version = "1" },
                equipment = new[] { new { name = "eq1", code = "eq1_code", type = "eq1_type" } }});

            var get = Get(ID, auth: Admin);
            RegisterForDeletion("/network/" + (int)get["network"]["id"]);
            RegisterForDeletion("/device/class/" + (int)get["deviceClass"]["id"]);
            RegisterForDeletion(ResourceUri + "/" + ID);

            Expect(get, Matches(new { id = ID, name = "_ut", network = new { name = "_ut_n_a" }, deviceClass = new {
                name = "_ut_dc_a", version = "1", equipment = new[] { new { name = "eq1", code = "eq1_code", type = "eq1_type" } } } }));
        }

        [Test]
        public void Create_Permanent()
        {
            // make device class permanent
            Client.Put("/device/class/" + DeviceClassID, new { isPermanent = true }, auth: Admin);
            
            // try to change device class
            Update(ID, new { key = "key", name = "_ut", network = new { name = "_ut_n" },
                deviceClass = new { name = "_ut_dc", version = "1", offlineTimeout = 10,
                equipment = new[] { new { name = "eq1", code = "eq1_code", type = "eq1_type" }}}});
            RegisterForDeletion(ResourceUri + "/" + ID);

            // permanent device classes should not change
            var dcResponse = Client.Get("/device/class/" + DeviceClassID, auth: Admin);
            Expect(dcResponse.Json, Matches(new { offlineTimeout = (int?)null,
                equipment = new[] { new { name = "_ut_eq", code = "_ut_eq", type = "_ut_eq" } } }));
        }

        [Test]
        public void Update()
        {
            Update(ID, new { key = "key", name = "_ut", network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } });

            // modify device, new network and device class should auto-create
            var obj = new { name = "_ut2", status = "status", data = new { a = "b" }, network = new { name = "_ut_n2", description = "desc" },
                deviceClass = new { name = "_ut_dc", version = "2", equipment = new[] { new { name = "_ut_eq", code = "_ut_eq", type = "_ut_eq" } } } };
            Update(ID, obj, auth: Admin);

            // expect valid server response
            var get = Get(ID, auth: Admin);
            RegisterForDeletion("/network/" + get["network"]["id"]);
            RegisterForDeletion("/device/class/" + get["deviceClass"]["id"]);
            RegisterForDeletion(ResourceUri + "/" + ID);
            Expect(get, Matches(obj));

            // verify device-update notification
            var notificationResponse = Client.Get("/device/" + ID + "/notification", auth: Admin);
            Expect(notificationResponse.Json.Count(), Is.GreaterThan(0));
            Expect(notificationResponse.Json.Last(), Matches(new { notification = "$device-update", parameters = obj }));
        }

        [Test]
        public void Update_Partial()
        {
            Update(ID, new { key = "key", name = "_ut", network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } });
            RegisterForDeletion(ResourceUri + "/" + ID);

            // modify device status only
            Update(ID, new { status = "status" }, auth: Admin);

            Expect(Get(ID, auth: Admin), Matches(new { id = ID, name = "_ut", status = "status",
                network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } }));
        }

        [Test]
        public void Update_DeviceAuth()
        {
            Update(ID, new { key = "key", name = "_ut", deviceClass = new { name = "_ut_dc", version = "1" } }, auth: Admin);
            RegisterForDeletion(ResourceUri + "/" + ID);

            // modify device properties (device authentication)
            Update(ID, new { status = "status", data = new { a = "b" }, network = new { name = "_ut_n" },
                deviceClass = new { name = "_ut_dc", version = "1", offlineTimeout = 10 } }, auth: Device(ID, "key"));

            Expect(Get(ID, auth: Admin), Matches(new { id = ID, name = "_ut", status = "status", data = new { a = "b" },
                network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1", offlineTimeout = 10 } }));
        }

        [Test]
        public void Update_ClientAuth()
        {
            Update(ID, new { key = "key", name = "_ut", network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } });
            RegisterForDeletion(ResourceUri + "/" + ID);

            // update an existing device with user authentication
            var user1 = CreateUser(1); // create a client user
            var user2 = CreateUser(1, NetworkID); // create a client user with access to network
            Expect(() => Update(ID, new { status = "status" }, auth: user1), FailsWith(401)); // should fail with 401
            Update(ID, new { status = "status", data = new { a = "b" },
                deviceClass = new { name = "_ut_dc", version = "1", offlineTimeout = 10 } }, auth: user2); // should succeed

            Expect(Get(ID, auth: Admin), Matches(new { id = ID, name = "_ut", status = "status", data = new { a = "b" },
                network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1", offlineTimeout = 10 } }));

            // access keys can update a device resource when have necessary permissions
            var accessKey1 = CreateAccessKey(user1, "RegisterDevice");
            var accessKey2 = CreateAccessKey(user2, "RegisterDevice", networks: new[] { 0 });
            var accessKey3 = CreateAccessKey(user2, "RegisterDevice", devices: new[] { Guid.NewGuid().ToString() });
            var accessKey4 = CreateAccessKey(user2, "RegisterDevice");
            Expect(() => Update(ID, new { status = "status2" }, auth: accessKey1), FailsWith(401)); // should fail with 401
            Expect(() => Update(ID, new { status = "status2" }, auth: accessKey2), FailsWith(401)); // should fail with 401
            Expect(() => Update(ID, new { status = "status2" }, auth: accessKey3), FailsWith(401)); // should fail with 401
            Update(ID, new { status = "status2" }, auth: accessKey4); // should succeed
            Expect(Get(ID, auth: Admin), Matches(new { status = "status2" }));
        }

        [Test]
        public void Delete()
        {
            var user1 = CreateUser(1); // create a client user
            var user2 = CreateUser(1, NetworkID); // create a client user with access to network
            Update(ID, new { key = "key", name = "_ut", network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } }, auth: Admin);
            RegisterForDeletion(ResourceUri + "/" + ID);
            
            Delete(ID, auth: user1); // delete should not succeed, but no error is raised
            Get(ID, auth: Admin);

            Delete(ID, auth: user2); // delete should succeed
            Expect(() => Get(ID, auth: Admin), FailsWith(404));
        }

        [Test]
        public void BadRequest()
        {
            Expect(() => Update(ID, new { name2 = "_ut" }, auth: Admin), FailsWith(400));
            Expect(() => Update(ID, new { key = "key", name = "_ut" }, auth: Admin), FailsWith(400));
            Expect(() => Update(ID, new { key = "key", name = "_ut", network = new { }, deviceClass = new { } }, auth: Admin), FailsWith(400));
        }

        [Test]
        public void Unauthorized()
        {
            // create a device
            Update(ID, new { key = "key", name = "_ut", network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } });
            RegisterForDeletion(ResourceUri + "/" + ID);

            // no authorization
            Expect(() => List(), FailsWith(401));
            Expect(() => Get(ID), FailsWith(401));
            Expect(() => Update(ID, new { status = "status" }), FailsWith(401));
            Expect(() => Delete(ID), FailsWith(401));

            // device authorization
            Expect(() => List(auth: Device(ID, "key")), FailsWith(401));
            Expect(() => Delete(ID, auth: Device(ID, "key")), FailsWith(401));
        }

        [Test]
        public void NotFound()
        {
            Expect(() => Get(Guid.NewGuid(), auth: Admin), FailsWith(404));
            Delete(Guid.NewGuid(), auth: Admin); // should not fail
        }
    }
}
