using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DeviceHive.Test.ApiTest
{
    [TestFixture]
    public class DeviceEquipmentTest : ResourceTest
    {
        private static readonly string DeviceGUID = "a97266f4-6e8a-4008-8242-022b49ea484f";
        private int? NetworkID { get; set; }
        private int? DeviceClassID { get; set; }
        private DateTime? Timestamp { get; set; }

        public DeviceEquipmentTest()
            : base("/device/" + DeviceGUID + "/equipment")
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

            var deviceResponse = Client.Put("/device/" + DeviceGUID, new { key = "key", name = "_ut_dc",
                network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } });
            Assert.That(deviceResponse.Status, Is.EqualTo(ExpectedUpdatedStatus));
            RegisterForDeletion("/device/" + DeviceGUID);

            var deviceNotificationResponse = Client.Post("/device/" + DeviceGUID + "/notification",
                new { notification = "equipment", parameters = new { equipment = "test", a = "b" } }, auth: Admin);
            Assert.That(deviceNotificationResponse.Status, Is.EqualTo(ExpectedCreatedStatus));
            Timestamp = (DateTime)deviceNotificationResponse.Json["timestamp"];
        }

        [Test]
        public void GetAll()
        {
            var user1 = CreateUser(1);
            var user2 = CreateUser(1, NetworkID);

            // user authentication
            Expect(() => List(auth: user1), FailsWith(404)); // should fail with 404
            var equipment = List(auth: user2); // should not fail
            Expect(equipment.Count, Is.EqualTo(1));
            Expect(equipment[0], Matches(new { id = "test", timestamp = Timestamp.Value, parameters = new { a = "b" } }));

            // access key authentication
            var accessKey1 = CreateAccessKey(user1, "GetDeviceState");
            var accessKey2 = CreateAccessKey(user2, "GetDeviceState", networkIds: new[] { 0 });
            var accessKey3 = CreateAccessKey(user2, "GetDeviceState", deviceGuids: new[] { Guid.NewGuid().ToString() });
            var accessKey4 = CreateAccessKey(user2, "GetDeviceState");
            Expect(() => List(auth: accessKey1), FailsWith(404)); // should fail with 404
            Expect(() => List(auth: accessKey2), FailsWith(404)); // should fail with 404
            Expect(() => List(auth: accessKey3), FailsWith(404)); // should fail with 404
            List(auth: accessKey4); // should succeed
        }

        [Test]
        public void Get()
        {
            var user1 = CreateUser(1);
            var user2 = CreateUser(1, NetworkID);

            // user authentication
            Expect(() => Get("test", auth: user1), FailsWith(404)); // should fail with 404
            var equipment = Get("test", auth: user2); // should not fail
            Expect(equipment, Matches(new { id = "test", timestamp = Timestamp.Value, parameters = new { a = "b" } }));

            // access key authentication
            var accessKey1 = CreateAccessKey(user1, "GetDeviceState");
            var accessKey2 = CreateAccessKey(user2, "GetDeviceState", networkIds: new[] { 0 });
            var accessKey3 = CreateAccessKey(user2, "GetDeviceState", deviceGuids: new[] { Guid.NewGuid().ToString() });
            var accessKey4 = CreateAccessKey(user2, "GetDeviceState");
            Expect(() => Get("test", auth: accessKey1), FailsWith(404)); // should fail with 404
            Expect(() => Get("test", auth: accessKey2), FailsWith(404)); // should fail with 404
            Expect(() => Get("test", auth: accessKey3), FailsWith(404)); // should fail with 404
            Get("test", auth: accessKey4); // should succeed
        }

        [Test]
        public void Create()
        {
            Expect(() => Create(new { parameters = new { x = "y" } }, auth: Admin), FailsWith(405));
        }

        [Test]
        public void Update()
        {
            Expect(() => Update("test", new { parameters = new { x = "y" } }, auth: Admin), FailsWith(405));
        }

        [Test]
        public void Delete()
        {
            Expect(() => Delete("test", auth: Admin), FailsWith(405));
        }

        [Test]
        public void Unauthorized()
        {
            // no authorization
            Expect(() => List(), FailsWith(401));
            Expect(() => Get("none"), FailsWith(401));
        }

        [Test]
        public void NotFound()
        {
            Expect(() => Get("none", auth: Admin), FailsWith(404));
        }
    }
}
