using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DeviceHive.Test.ApiTest
{
    [TestFixture]
    public class DeviceNotificationTest : ResourceTest
    {
        private static readonly string DeviceGUID = "a97266f4-6e8a-4008-8242-022b49ea484f";
        private int? NetworkID { get; set; }
        private int? DeviceClassID { get; set; }

        public DeviceNotificationTest()
            : base("/device/" + DeviceGUID + "/notification")
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
        }

        [Test]
        public void GetAll()
        {
            Get(auth: Admin);
        }

        [Test]
        public void GetAll_Filter()
        {
            var resource = Create(new { notification = "_ut" }, auth: Device(DeviceGUID, "key"));

            Expect(Get(new Dictionary<string, string> { { "start", DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ss.ffffff") } }, auth: Admin).Count, Is.GreaterThan(0));
            Expect(Get(new Dictionary<string, string> { { "start", DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss.ffffff") } }, auth: Admin).Count, Is.EqualTo(0));
            Expect(Get(new Dictionary<string, string> { { "end", DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ss.ffffff") } }, auth: Admin).Count, Is.EqualTo(0));
            Expect(Get(new Dictionary<string, string> { { "end", DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss.ffffff") } }, auth: Admin).Count, Is.GreaterThan(0));
        }

        [Test]
        public void Get_Client()
        {
            var user1 = CreateUser(1);
            var user2 = CreateUser(1, NetworkID);
            var resource = Create(new { notification = "_ut" }, auth: Device(DeviceGUID, "key"));

            Expect(() => Get(resource, auth: user1), FailsWith(404)); // should fail with 404
            Get(resource, auth: user2); // should succeed
        }

        [Test]
        public void Poll()
        {
            // task to poll new resources
            var poll = new Task(() =>
                {
                    var response = Client.Get(ResourceUri + "/poll", auth: Admin);
                    Expect(response.Status, Is.EqualTo(200));
                    Expect(response.Json, Is.InstanceOf<JArray>());

                    var result =  (JArray)response.Json;
                    Expect(result.Count, Is.EqualTo(1));
                    Expect(result[0], Matches(new { notification = "_ut2" }));
                });

            // create resource, start poll, wait, then create another resource
            var resource1 = Create(new { notification = "_ut1" }, auth: Device(DeviceGUID, "key"));
            poll.Start();
            Thread.Sleep(100);
            var resource2 = Create(new { notification = "_ut2" }, auth: Device(DeviceGUID, "key"));

            Expect(poll.Wait(2000), Is.True); // task should complete
        }

        [Test]
        public void Create()
        {
            var resource = Create(new { notification = "_ut" }, auth: Device(DeviceGUID, "key"));

            Expect(resource, Matches(new { notification = "_ut", parameters = (string)null, timestamp = ResponseMatchesContraint.Timestamp }));
            Expect(Get(resource, auth: Admin), Matches(new { notification = "_ut", parameters = (string)null, timestamp = ResponseMatchesContraint.Timestamp }));
        }

        [Test]
        public void Update()
        {
            var resource = Create(new { notification = "_ut" }, auth: Device(DeviceGUID, "key"));
            
            Expect(() => Update(resource, new { notification = "_ut2", parameters = new { a = "b" } }, auth: Admin), FailsWith(405));
        }

        [Test]
        public void Delete()
        {
            var resource = Create(new { notification = "_ut" }, auth: Device(DeviceGUID, "key"));

            Expect(() => { Delete(resource, auth: Admin); return false; }, FailsWith(405));
        }

        [Test]
        public void BadRequest()
        {
            Expect(() => Create(new { notification2 = "_ut" }, auth: Device(DeviceGUID, "key")), FailsWith(400));
        }

        [Test]
        public void Unauthorized()
        {
            // no authorization
            Expect(() => Get(), FailsWith(401));
            Expect(() => Get(UnexistingResourceID), FailsWith(401));
            Expect(() => Create(new { notification = "_ut" }), FailsWith(401));

            // user authorization
            var user = CreateUser(1, NetworkID);
            Expect(() => Create(new { notification = "_ut" }, auth: user), FailsWith(401));
        }

        [Test]
        public void NotFound()
        {
            Expect(() => Get(UnexistingResourceID, auth: Admin), FailsWith(404));
        }
    }
}
