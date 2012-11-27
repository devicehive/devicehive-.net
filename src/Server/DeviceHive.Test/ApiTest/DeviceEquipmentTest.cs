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

            var deviceResponse = Client.Put("/device/" + DeviceGUID, new { key = "key", name = "_ut_dc", network = NetworkID, deviceClass = DeviceClassID });
            Assert.That(deviceResponse.Status, Is.EqualTo(200));
            RegisterForDeletion("/device/" + DeviceGUID);

            var deviceNotificationResponse = Client.Post("/device/" + DeviceGUID + "/notification",
                new { notification = "equipment", parameters = new { equipment = "test", a = "b" } }, auth: Admin);
            Assert.That(deviceNotificationResponse.Status, Is.EqualTo(ExpectedCreatedStatus));
            Timestamp = (DateTime)deviceNotificationResponse.Json["timestamp"];
        }

        [Test]
        public void GetAll()
        {
            Expect(Get(auth: Admin).Count, Is.EqualTo(1));
        }

        [Test]
        public void GetByCode()
        {
            Expect(Get("test", auth: Admin), Matches(new { id = "test", timestamp = Timestamp.Value, parameters = new { a = "b" } }));
        }

        [Test]
        public void Get_Client()
        {
            var user1 = CreateUser(1);
            var user2 = CreateUser(1, NetworkID);

            Expect(() => Get(auth: user1), FailsWith(404)); // should fail with 404
            Get(auth: user2); // should succeed
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
            Expect(() => { Delete("test", auth: Admin); return false; }, FailsWith(405));
        }

        [Test]
        public void Unauthorized()
        {
            // no authorization
            Expect(() => Get(), FailsWith(401));
            Expect(() => Get("none"), FailsWith(401));
        }

        [Test]
        public void NotFound()
        {
            Expect(() => Get("none", auth: Admin), FailsWith(404));
        }
    }
}
