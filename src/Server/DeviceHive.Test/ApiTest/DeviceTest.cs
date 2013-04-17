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

            var deviceClassResponse = Client.Post("/device/class", new { name = "_ut_dc", version = "1" }, auth: Admin);
            Assert.That(deviceClassResponse.Status, Is.EqualTo(ExpectedCreatedStatus));
            DeviceClassID = (int)deviceClassResponse.Json["id"];
            RegisterForDeletion("/device/class/" + DeviceClassID);
        }

        [Test]
        public void GetAll()
        {
            // create new device
            var user = CreateUser(1, NetworkID);
            Update(ID, new { key = "key", name = "_ut", network = NetworkID, deviceClass = DeviceClassID }, auth: Admin);
            RegisterForDeletion(ResourceUri + "/" + ID);

            // admin: get device by name
            var devices = Get(new Dictionary<string, string> { { "name", "_ut" } }, auth: Admin);
            Expect(devices.Count, Is.EqualTo(1));
            Expect(devices[0], Matches(new { id = ID }));

            // admin: get device by network
            devices = Get(new Dictionary<string, string> { { "networkId", NetworkID.ToString() } }, auth: Admin);
            Expect(devices.Count, Is.EqualTo(1));
            Expect(devices[0], Matches(new { id = ID }));

            // admin: get device by device class
            devices = Get(new Dictionary<string, string> { { "deviceClassId", DeviceClassID.ToString() } }, auth: Admin);
            Expect(devices.Count, Is.EqualTo(1));
            Expect(devices[0], Matches(new { id = ID }));

            // user: get all devices
            devices = Get(auth: user);
            Expect(devices.Count, Is.EqualTo(1));
            Expect(devices[0], Matches(new { id = ID }));

            // user: get non-existing device by name
            devices = Get(new Dictionary<string, string> { { "name", "nonexist" } }, auth: user);
            Expect(devices.Count, Is.EqualTo(0));
        }

        [Test]
        public void Get_Client()
        {
            // create new device
            var user1 = CreateUser(1); // create a client user
            var user2 = CreateUser(1, NetworkID); // create a client user with access to network
            Update(ID, new { key = "key", name = "_ut", network = NetworkID, deviceClass = DeviceClassID }, auth: Admin);
            RegisterForDeletion(ResourceUri + "/" + ID);

            // clients can receive device resource when associated with the network
            Expect(() => Get(ID, auth: user1), FailsWith(404)); // should fail with 404
            var device = Get(ID, auth: user2); // should succeed
            Expect(device["network"]["key"], Is.Null); // verify that network does not include key
        }

        [Test]
        public void Get_Device()
        {
            // create new device
            Update(ID, new { key = "key", name = "_ut", network = NetworkID, deviceClass = DeviceClassID }, auth: Admin);
            RegisterForDeletion(ResourceUri + "/" + ID);

            // devices can receive device resource when specify a valid key
            Expect(() => Get(ID, auth: Device(ID, "wrong_key")), FailsWith(401)); // should fail with 401
            var device = Get(ID, auth: Device(ID, "key")); // should succeed
            Expect(device["network"]["key"], Is.Null); // verify that network does not include key
        }

        [Test]
        public void Create()
        {
            // create new device
            Update(ID, new { key = "key", name = "_ut", network = NetworkID, deviceClass = DeviceClassID });
            RegisterForDeletion(ResourceUri + "/" + ID);

            // expect valid server response
            Expect(Get(ID, auth: Admin), Matches(new { id = ID, name = "_ut", network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } }));
            
            // verify device-add notification
            var notificationResponse = Client.Get("/device/" + ID + "/notification", auth: Admin);
            Expect(notificationResponse.Json.Count(), Is.EqualTo(1));
            Expect(notificationResponse.Json[0], Matches(new { notification = "$device-add", parameters =
                new { id = ID, name = "_ut", network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } } }));
        }

        [Test]
        public void Create_RefByName()
        {
            // network matches by name, device class matches by name and version
            Update(ID, new { key = "key", name = "_ut", network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } });
            RegisterForDeletion(ResourceUri + "/" + ID);

            Expect(Get(ID, auth: Admin), Matches(new { id = ID, name = "_ut", network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } }));
        }

        [Test]
        public void Create_RefByNameAndKey()
        {
            // set a key to the network
            Client.Put("/network/" + NetworkID, new { key = "network_key" }, auth: Admin);

            // referencing network without key is not allowed
            RegisterForDeletion(ResourceUri + "/" + ID);
            Expect(() => { Update(ID, new { key = "key", name = "_ut", network = NetworkID, deviceClass = DeviceClassID }); return false; }, FailsWith(403));
            Expect(() => { Update(ID, new { key = "key", name = "_ut", network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } }); return false; }, FailsWith(403));

            // network matches by name, device class matches by name and version
            Update(ID, new { key = "key", name = "_ut", network = new { name = "_ut_n", key = "network_key" }, deviceClass = DeviceClassID });

            Expect(Get(ID, auth: Admin), Matches(new { id = ID, name = "_ut", network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } }));
        }

        [Test]
        public void Create_RefCreate()
        {
            // both network and device class auto-create
            Update(ID, new { key = "key", name = "_ut", network = new { name = "_ut_n_a" }, deviceClass = new { name = "_ut_dc_a", version = "1" } });

            var get = Get(ID, auth: Admin);
            RegisterForDeletion("/network/" + (int)get["network"]["id"]);
            RegisterForDeletion("/device/class/" + (int)get["deviceClass"]["id"]);
            RegisterForDeletion(ResourceUri + "/" + ID);

            Expect(get, Matches(new { id = ID, name = "_ut",
                network = new { name = "_ut_n_a" }, deviceClass = new { name = "_ut_dc_a", version = "1" } }));
        }

        [Test]
        public void Create_WithEquipment()
        {
            Update(ID, new { key = "key", name = "_ut", network = new { name = "_ut_n_a" }, deviceClass = new { name = "_ut_dc_a", version = "1" },
                equipment = new[] { new { name = "eq1", code = "eq1_code", type = "eq1_type" } }});

            var get = Get(ID, auth: Admin);
            var networkId = (int)get["network"]["id"];
            var deviceClassId = (int)get["deviceClass"]["id"];
            RegisterForDeletion("/network/" + networkId);
            RegisterForDeletion("/device/class/" + deviceClassId);
            RegisterForDeletion(ResourceUri + "/" + ID);

            var eqResponse = Client.Get("/device/class/" + deviceClassId, auth: Admin);
            var equipment = eqResponse.Json["equipment"][0];
            var equipmentId = (int)equipment["id"];
            RegisterForDeletion("/device/class/" + deviceClassId + "/equipment/" + equipmentId);

            Expect(get, Matches(new { id = ID, name = "_ut",
                network = new { name = "_ut_n_a" }, deviceClass = new { name = "_ut_dc_a", version = "1" } }));
            Expect(equipment, Matches(new { name = "eq1", code = "eq1_code", type = "eq1_type" }));
        }

        [Test]
        public void Create_Permanent()
        {
            // make device class permanent
            Client.Put("/device/class/" + DeviceClassID, new { isPermanent = true }, auth: Admin);
            
            Update(ID, new { key = "key", name = "_ut", network = new { name = "_ut_n" },
                deviceClass = new { name = "_ut_dc", version = "1", offlineTimeout = 10 },
                equipment = new[] { new { name = "eq1", code = "eq1_code", type = "eq1_type" }}});
            RegisterForDeletion(ResourceUri + "/" + ID);

            var dcResponse = Client.Get("/device/class/" + DeviceClassID, auth: Admin);
            var equipment = (JArray)dcResponse.Json["equipment"];
            Expect(dcResponse.Json, Matches(new { offlineTimeout = (int?)null }));
            Expect(equipment.Count, Is.EqualTo(0)); // permanent device classes should not change
        }

        [Test]
        public void Update()
        {
            // modified network and device class auto-create
            Update(ID, new { key = "key", name = "_ut", network = NetworkID, deviceClass = DeviceClassID });

            // modify device
            var obj = new { name = "_ut2", status = "status", data = new { a = "b" },
                network = new { name = "_ut_n2", description = "desc" }, deviceClass = new { name = "_ut_dc", version = "2" } };
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
            Update(ID, new { key = "key", name = "_ut", network = NetworkID, deviceClass = DeviceClassID });
            RegisterForDeletion(ResourceUri + "/" + ID);

            // modify device status only
            Update(ID, new { status = "status" }, auth: Admin);

            Expect(Get(ID, auth: Admin), Matches(new { id = ID, name = "_ut", status = "status",
                network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } }));
        }

        [Test]
        public void Update_DeviceAuth()
        {
            Update(ID, new { key = "key", name = "_ut", deviceClass = DeviceClassID }, auth: Admin);
            RegisterForDeletion(ResourceUri + "/" + ID);

            // modify device properties (device authentication)
            Update(ID, new { status = "status", data = new { a = "b" }, network = NetworkID,
                deviceClass = new { name = "_ut_dc", version = "1", offlineTimeout = 10 } }, auth: Device(ID, "key"));

            Expect(Get(ID, auth: Admin), Matches(new { id = ID, name = "_ut", status = "status", data = new { a = "b" },
                network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1", offlineTimeout = 10 } }));
        }

        [Test]
        public void Delete()
        {
            Update(ID, new { key = "key", name = "_ut", network = NetworkID, deviceClass = DeviceClassID }, auth: Admin);
            RegisterForDeletion(ResourceUri + "/" + ID);

            Delete(ID, auth: Admin);

            Expect(() => Get(ID, auth: Admin), FailsWith(404));
        }

        [Test]
        public void BadRequest()
        {
            Expect(() => { Update(ID, new { name2 = "_ut" }, auth: Admin); return false; }, FailsWith(400));
            Expect(() => { Update(ID, new { key = "key", name = "_ut" }, auth: Admin); return false; }, FailsWith(400));
            Expect(() => { Update(ID, new { key = "key", name = "_ut", network = UnexistingResourceID, deviceClass = UnexistingResourceID }, auth: Admin); return false; }, FailsWith(400));
        }

        [Test]
        public void Unauthorized()
        {
            // create a device
            Update(ID, new { key = "key", name = "_ut", network = NetworkID, deviceClass = DeviceClassID });
            RegisterForDeletion(ResourceUri + "/" + ID);

            // no authorization
            Expect(() => Get(), FailsWith(401));
            Expect(() => Get(ID), FailsWith(401));
            Expect(() => { Update(ID, new { status = "status" }); return false; }, FailsWith(401));
            Expect(() => { Delete(ID); return false; }, FailsWith(401));

            // user authorization
            var user = CreateUser(1, NetworkID);
            Expect(() => { Update(ID, new { status = "status" }, auth: user); return false; }, FailsWith(401));
            Expect(() => { Delete(ID, auth: user); return false; }, FailsWith(401));
        }

        [Test]
        public void NotFound()
        {
            Expect(() => Get(Guid.NewGuid(), auth: Admin), FailsWith(404));
            Delete(Guid.NewGuid(), auth: Admin); // should not fail
        }
    }
}
