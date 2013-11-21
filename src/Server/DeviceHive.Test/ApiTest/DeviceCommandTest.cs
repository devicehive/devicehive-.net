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

            var deviceResponse = Client.Put("/device/" + DeviceGUID, new { key = "key", name = "_ut_dc",
                network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } });
            Assert.That(deviceResponse.Status, Is.EqualTo(ExpectedUpdatedStatus));
            RegisterForDeletion("/device/" + DeviceGUID);
        }

        [Test]
        public void GetAll()
        {
            // create command resources
            var user = CreateUser(1, NetworkID);
            var resource1 = Create(new { command = "_ut1" }, auth: user);
            var resource2 = Create(new { command = "_ut2" }, auth: user);

            // user: get all commands
            var commands = List(auth: user);
            Expect(commands.Count, Is.EqualTo(2));

            // user: get commands by name
            commands = List(new Dictionary<string, string> { { "command", "_ut1" } }, auth: user);
            Expect(commands.Count, Is.EqualTo(1));
            Expect(GetResourceId(commands[0]), Is.EqualTo(GetResourceId(resource1)));

            // user: get commands by start date
            commands = List(new Dictionary<string, string> { { "start", DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ss.ffffff") } }, auth: user);
            Expect(commands.Count, Is.EqualTo(2));

            commands = List(new Dictionary<string, string> { { "start", DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss.ffffff") } }, auth: user);
            Expect(commands.Count, Is.EqualTo(0));

            // user: get commands by end date
            commands = List(new Dictionary<string, string> { { "end", DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ss.ffffff") } }, auth: user);
            Expect(commands.Count, Is.EqualTo(0));

            commands = List(new Dictionary<string, string> { { "end", DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss.ffffff") } }, auth: user);
            Expect(commands.Count, Is.EqualTo(2));
        }

        [Test]
        public void Get()
        {
            // create resource
            var resource = Create(new { command = "_ut" }, auth: Admin);

            // device authentication
            Get(resource, auth: Device(DeviceGUID, "key")); // should succeed

            // user authentication
            var user1 = CreateUser(1);
            var user2 = CreateUser(1, NetworkID);
            Expect(() => Get(resource, auth: user1), FailsWith(404)); // should fail with 404
            Get(resource, auth: user2); // should succeed

            // access key authentication
            var accessKey1 = CreateAccessKey(user1, "GetDeviceCommand");
            var accessKey2 = CreateAccessKey(user2, "GetDeviceCommand", networkIds: new[] { 0 });
            var accessKey3 = CreateAccessKey(user2, "GetDeviceCommand", deviceGuids: new[] { Guid.NewGuid().ToString() });
            var accessKey4 = CreateAccessKey(user2, "GetDeviceCommand");
            Expect(() => Get(resource, auth: accessKey1), FailsWith(404)); // should fail with 404
            Expect(() => Get(resource, auth: accessKey2), FailsWith(404)); // should fail with 404
            Expect(() => Get(resource, auth: accessKey3), FailsWith(404)); // should fail with 404
            Get(resource, auth: accessKey4); // should succeed
        }

        [Test]
        public void Poll()
        {
            // create user account
            var user = CreateUser(1, NetworkID);

            // create resource
            var resource1 = Create(new { command = "_ut1" }, auth: user);

            // task to poll new resources
            var poll = new Task(() =>
                {
                    var response = Client.Get(ResourceUri + "/poll?names=_ut1", auth: user);
                    Expect(response.Status, Is.EqualTo(200));
                    Expect(response.Json, Is.InstanceOf<JArray>());

                    var result = (JArray)response.Json;
                    Expect(result.Count, Is.EqualTo(1));
                    Expect(result[0], Matches(new { command = "_ut1" }));
                });

            // start poll, wait, then create resources
            poll.Start();
            Thread.Sleep(100);
            var resource2 = Create(new { command = "_ut2" }, auth: user);
            Thread.Sleep(100);
            var resource3 = Create(new { command = "_ut1" }, auth: user);

            Expect(poll.Wait(2000), Is.True); // task should complete
        }

        [Test]
        public void Poll_NoWait()
        {
            // create user account
            var user = CreateUser(1, NetworkID);

            // task to poll new resources
            var poll = Task.Factory.StartNew(() =>
                {
                    var response = Client.Get(ResourceUri + "/poll?waitTimeout=0", auth: user);
                    Expect(response.Status, Is.EqualTo(200));
                    Expect(response.Json, Is.InstanceOf<JArray>());
                    Expect(response.Json.Count(), Is.EqualTo(0));
                });

            Expect(poll.Wait(2000), Is.True); // task should complete immediately
        }

        [Test]
        public void PollMany()
        {
            // create user account
            var user = CreateUser(1, NetworkID);

            // task to poll new resources
            var poll = new Task(() =>
                {
                    var response = Client.Get("/device/command/poll?names=_ut1&deviceGuids=" + DeviceGUID, auth: user);
                    Expect(response.Status, Is.EqualTo(200));
                    Expect(response.Json, Is.InstanceOf<JArray>());

                    var result = (JArray)response.Json;
                    Expect(result.Count, Is.EqualTo(1));
                    Expect(result[0], Matches(new { deviceGuid = DeviceGUID, command = new { command = "_ut1" } }));
                });

            // create resource, start poll, wait, then create resources
            var resource1 = Create(new { command = "_ut1" }, auth: user);
            poll.Start();
            Thread.Sleep(100);
            var resource2 = Create(new { command = "_ut2" }, auth: user);
            Thread.Sleep(100);
            var resource3 = Create(new { command = "_ut1" }, auth: user);

            Expect(poll.Wait(2000), Is.True); // task should complete
        }

        [Test]
        public void PollMany_OtherDevice()
        {
            // create another network and device
            var otherNetworkResponse = Client.Post("/network", new { name = "_ut_n2" }, auth: Admin);
            Assert.That(otherNetworkResponse.Status, Is.EqualTo(ExpectedCreatedStatus));
            var otherNetworkID = (int)otherNetworkResponse.Json["id"];
            RegisterForDeletion("/network/" + otherNetworkID);

            var otherDeviceGuid = Guid.NewGuid().ToString();
            var otherDeviceResponse = Client.Put("/device/" + otherDeviceGuid, new { key = "key", name = "_ut_dc2",
                network = new { name = "_ut_n2" }, deviceClass = new { name = "_ut_dc", version = "1" } });
            Assert.That(otherDeviceResponse.Status, Is.EqualTo(ExpectedUpdatedStatus));
            RegisterForDeletion("/device/" + otherDeviceGuid);

            // task to poll new resources
            var user = CreateUser(1, NetworkID);
            var poll = new Task(() =>
                {
                    var response = Client.Get("/device/command/poll", auth: user);
                    Expect(response.Status, Is.EqualTo(200));
                    Expect(response.Json, Is.InstanceOf<JArray>());

                    var result = (JArray)response.Json;
                    Expect(result.Count, Is.EqualTo(1));
                    Expect(result[0], Matches(new { deviceGuid = DeviceGUID, command = new { command = "_ut2" } }));
                });

            // start poll, wait, create other response, wait, then create matching resource
            poll.Start();
            Thread.Sleep(100);
            var response1 = Client.Post("/device/" + otherDeviceGuid + "/command", new { command = "_ut2" }, auth: Admin);
            Assert.That(response1.Status, Is.EqualTo(ExpectedCreatedStatus));
            Thread.Sleep(100);
            var resource2 = Create(new { command = "_ut2" }, auth: Admin);

            Expect(poll.Wait(2000), Is.True); // task should complete
        }

        [Test]
        public void PollMany_NoWait()
        {
            // create user account
            var user = CreateUser(1, NetworkID);

            // task to poll new resources
            var poll = Task.Factory.StartNew(() =>
                {
                    var response = Client.Get("/device/command/poll?waitTimeout=0", auth: user);
                    Expect(response.Status, Is.EqualTo(200));
                    Expect(response.Json, Is.InstanceOf<JArray>());
                    Expect(response.Json.Count(), Is.EqualTo(0));
                });

            Expect(poll.Wait(2000), Is.True); // task should complete immediately
        }

        [Test]
        public void Poll_ByID()
        {
            // create user account
            var user = CreateUser(1, NetworkID);

            // create resource
            var resource = Create(new { command = "_ut1" }, auth: Admin);

            // task to poll command update
            var poll = new Task(() =>
            {
                var response = Client.Get(ResourceUri + "/" + GetResourceId(resource) + "/poll", auth: user);
                Expect(response.Status, Is.EqualTo(200));
                Expect(response.Json, Is.InstanceOf<JObject>());

                var result = (JObject)response.Json;
                Expect(result, Matches(new { command = "_ut1", status = "Done", result = "OK" }));
            });

            // start poll, wait, then update resource
            poll.Start();
            Thread.Sleep(100);
            Update(resource, new { status = "Done", result = "OK" }, auth: user);

            Expect(poll.Wait(2000), Is.True); // task should complete
        }

        [Test]
        public void Create()
        {
            // user authorization
            var user1 = CreateUser(1);
            var user2 = CreateUser(1, NetworkID);
            Expect(() => Create(new { command = "_ut" }, auth: user1), FailsWith(404)); // should fail
            var resource = Create(new { command = "_ut" }, auth: user2); // should succeed
            Expect(Get(resource, auth: Admin), Matches(new { command = "_ut", parameters = (string)null, status = (string)null, result = (string)null, timestamp = ResponseMatchesContraint.Timestamp, userId = user2.ID }));

            // access keys authorization
            var accessKey1 = CreateAccessKey(user1, "CreateDeviceCommand");
            var accessKey2 = CreateAccessKey(user2, "CreateDeviceCommand", networkIds: new[] { 0 });
            var accessKey3 = CreateAccessKey(user2, "CreateDeviceCommand", deviceGuids: new[] { Guid.NewGuid().ToString() });
            var accessKey4 = CreateAccessKey(user2, "CreateDeviceCommand");
            Expect(() => Create(new { command = "_ut" }, auth: accessKey1), FailsWith(404)); // should fail with 404
            Expect(() => Create(new { command = "_ut" }, auth: accessKey2), FailsWith(404)); // should fail with 404
            Expect(() => Create(new { command = "_ut" }, auth: accessKey3), FailsWith(404)); // should fail with 404
            Create(new { command = "_ut" }, auth: accessKey4); // should succeed
        }

        [Test]
        public void Update()
        {
            var resource = Create(new { command = "_ut" }, auth: Admin);

            // device authorization
            Update(resource, new { parameters = new { a = "b" }, status = "OK", result = "Success" }, auth: Device(DeviceGUID, "key"));
            Expect(Get(resource, auth: Admin), Matches(new { parameters = new { a = "b" }, status = "OK", result = "Success", timestamp = ResponseMatchesContraint.Timestamp }));

            // user authorization
            var user1 = CreateUser(1);
            var user2 = CreateUser(1, NetworkID);
            Expect(() => Update(resource, new { command = "_ut1" }, auth: user1), FailsWith(404)); // should fail
            Update(resource, new { command = "_ut1" }, auth: user2); // should succeed

            // access keys authorization
            var accessKey1 = CreateAccessKey(user1, "UpdateDeviceCommand");
            var accessKey2 = CreateAccessKey(user2, "UpdateDeviceCommand", networkIds: new[] { 0 });
            var accessKey3 = CreateAccessKey(user2, "UpdateDeviceCommand", deviceGuids: new[] { Guid.NewGuid().ToString() });
            var accessKey4 = CreateAccessKey(user2, "UpdateDeviceCommand");
            Expect(() => Update(resource, new { command = "_ut1" }, auth: accessKey1), FailsWith(404)); // should fail with 404
            Expect(() => Update(resource, new { command = "_ut1" }, auth: accessKey2), FailsWith(404)); // should fail with 404
            Expect(() => Update(resource, new { command = "_ut1" }, auth: accessKey3), FailsWith(404)); // should fail with 404
            Update(resource, new { command = "_ut1" }, auth: accessKey4); // should succeed
        }

        [Test]
        public void Delete()
        {
            // delete is not allwed
            var resource = Create(new { command = "_ut" }, auth: Admin);
            Expect(() => Delete(resource, auth: Admin), FailsWith(405));
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
            Expect(() => List(), FailsWith(401));
            Expect(() => Get(UnexistingResourceID), FailsWith(401));
            Expect(() => Create(new { command = "_ut" }), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { command = "_ut" }), FailsWith(401));

            // device authorization
            Expect(() => Create(new { command = "_ut" }, auth: Device(DeviceGUID, "key")), FailsWith(401));
        }

        [Test]
        public void NotFound()
        {
            Expect(() => Get(UnexistingResourceID, auth: Admin), FailsWith(404));
            Expect(() => Update(UnexistingResourceID, new { command = "_ut" }, auth: Admin), FailsWith(404));
        }
    }
}
