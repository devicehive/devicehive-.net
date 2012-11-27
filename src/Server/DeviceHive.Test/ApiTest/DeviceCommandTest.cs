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
    public class DeviceCommandTest : ResourceTest
    {
        private static readonly string DeviceGUID = "a97266f4-6e8a-4008-8242-022b49ea484f";
        private int? NetworkID { get; set; }
        private int? DeviceClassID { get; set; }

        public DeviceCommandTest()
            : base("/device/" + DeviceGUID + "/command")
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
            Get(auth: Device(DeviceGUID, "key"));
        }

        [Test]
        public void GetAll_Filter()
        {
            var resource = Create(new { command = "_ut" }, auth: Admin);

            Expect(Get(new Dictionary<string, string> { { "start", DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ss.ffffff") } }, auth: Device(DeviceGUID, "key")).Count, Is.GreaterThan(0));
            Expect(Get(new Dictionary<string, string> { { "start", DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss.ffffff") } }, auth: Device(DeviceGUID, "key")).Count, Is.EqualTo(0));
            Expect(Get(new Dictionary<string, string> { { "end", DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ss.ffffff") } }, auth: Device(DeviceGUID, "key")).Count, Is.EqualTo(0));
            Expect(Get(new Dictionary<string, string> { { "end", DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss.ffffff") } }, auth: Device(DeviceGUID, "key")).Count, Is.GreaterThan(0));
        }

        [Test]
        public void Get_Client()
        {
            var user1 = CreateUser(1);
            var user2 = CreateUser(1, NetworkID);
            var resource = Create(new { command = "_ut" }, auth: Admin);

            Expect(() => Get(resource, auth: user1), FailsWith(404)); // should fail with 404
            Get(resource, auth: user2); // should succeed
        }

        [Test]
        public void Poll()
        {
            // task to poll new resources
            var poll = new Task(() =>
                {
                    var response = Client.Get(ResourceUri + "/poll", auth: Device(DeviceGUID, "key"));
                    Expect(response.Status, Is.EqualTo(200));
                    Expect(response.Json, Is.InstanceOf<JArray>());

                    var result = (JArray)response.Json;
                    Expect(result.Count, Is.EqualTo(1));
                    Expect(result[0], Matches(new { command = "_ut2" }));
                });

            // create resource, start poll, wait, then create another resource
            var resource1 = Create(new { command = "_ut1" }, auth: Admin);
            poll.Start();
            Thread.Sleep(100);
            var resource2 = Create(new { command = "_ut2" }, auth: Admin);

            Expect(poll.Wait(2000), Is.True); // task should complete
        }

        [Test]
        public void Create()
        {
            var resource = Create(new { command = "_ut" }, auth: Admin);

            Expect(resource, Matches(new { command = "_ut", parameters = (string)null, status = (string)null, result = (string)null, timestamp = ResponseMatchesContraint.Timestamp }));
            Expect(Get(resource, auth: Admin), Matches(new { command = "_ut", parameters = (string)null, status = (string)null, result = (string)null, timestamp = ResponseMatchesContraint.Timestamp }));
        }

        [Test]
        public void Update()
        {
            var resource = Create(new { command = "_ut" }, auth: Admin);
            var update = Update(resource, new { command = "_ut2", parameters = new { a = "b" }, status = "OK", result = "Success" }, auth: Device(DeviceGUID, "key"));

            Expect(update, Matches(new { command = "_ut2", parameters = new { a = "b" }, status = "OK", result = "Success", timestamp = ResponseMatchesContraint.Timestamp }));
            Expect(Get(resource, auth: Admin), Matches(new { command = "_ut2", parameters = new { a = "b" }, status = "OK", result = "Success", timestamp = ResponseMatchesContraint.Timestamp }));
        }

        [Test]
        public void Update_Partial()
        {
            var resource = Create(new { command = "_ut", parameters = new { a = "b" } }, auth: Admin);
            var update = Update(resource, new { parameters = new { a = "b2" } }, auth: Device(DeviceGUID, "key"));

            Expect(update, Matches(new { command = "_ut", parameters = new { a = "b2" }, timestamp = ResponseMatchesContraint.Timestamp }));
            Expect(Get(resource, auth: Admin), Matches(new { command = "_ut", parameters = new { a = "b2" }, timestamp = ResponseMatchesContraint.Timestamp }));
        }

        [Test]
        public void Delete()
        {
            var resource = Create(new { command = "_ut" }, auth: Admin);

            Expect(() => { Delete(resource, auth: Admin); return false; }, FailsWith(405));
        }

        [Test]
        public void BadRequest()
        {
            Expect(() => Create(new { command2 = "_ut" }, auth: Admin), FailsWith(400));
        }

        [Test]
        public void Unauthorized()
        {
            // no authorization
            Expect(() => Get(), FailsWith(401));
            Expect(() => Get(UnexistingResourceID), FailsWith(401));
            Expect(() => Create(new { command = "_ut" }), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { command = "_ut" }), FailsWith(401));

            // user authorization
            var user = CreateUser(1, NetworkID);
            Expect(() => Update(UnexistingResourceID, new { notification = "_ut" }, auth: user), FailsWith(401));
        }

        [Test]
        public void NotFound()
        {
            Expect(() => Get(UnexistingResourceID, auth: Admin), FailsWith(404));
            Expect(() => Update(UnexistingResourceID, new { command = "_ut" }, auth: Admin), FailsWith(404));
        }
    }
}
