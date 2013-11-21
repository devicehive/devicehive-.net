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

            var deviceResponse = Client.Put("/device/" + DeviceGUID, new { key = "key", name = "_ut_dc",
                network = new { name = "_ut_n" }, deviceClass = new { name = "_ut_dc", version = "1" } });
            Assert.That(deviceResponse.Status, Is.EqualTo(ExpectedUpdatedStatus));
            RegisterForDeletion("/device/" + DeviceGUID);
        }

        [Test]
        public void GetAll()
        {
            // create notification resources
            var user = CreateUser(1, NetworkID);
            var resource1 = Create(new { notification = "_ut1" }, auth: user);
            var resource2 = Create(new { notification = "_ut2" }, auth: user);
            var resource3 = Create(new { notification = "_ut2" }, auth: user);

            // user: get all notifications
            var notifications = List(auth: user);
            Expect(notifications.Count, Is.EqualTo(4)); // adding device creation notification

            // user: get notifications with grid interval
            notifications = List(new Dictionary<string, string> { { "gridInterval", Convert.ToString(24 * 3600) } }, auth: user);
            Expect(notifications.Count, Is.EqualTo(3));

            // user: get notifications by name
            notifications = List(new Dictionary<string, string> { { "notification", "_ut1" } }, auth: user);
            Expect(notifications.Count, Is.EqualTo(1));
            Expect(GetResourceId(notifications[0]), Is.EqualTo(GetResourceId(resource1)));

            // user: get notifications by start date
            notifications = List(new Dictionary<string, string> { { "start", DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ss.ffffff") } }, auth: user);
            Expect(notifications.Count, Is.GreaterThanOrEqualTo(2));

            notifications = List(new Dictionary<string, string> { { "start", DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss.ffffff") } }, auth: user);
            Expect(notifications.Count, Is.EqualTo(0));

            // user: get notifications by end date
            notifications = List(new Dictionary<string, string> { { "end", DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ss.ffffff") } }, auth: user);
            Expect(notifications.Count, Is.EqualTo(0));

            notifications = List(new Dictionary<string, string> { { "end", DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss.ffffff") } }, auth: user);
            Expect(notifications.Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void Get()
        {
            // create resource
            var resource = Create(new { notification = "_ut" }, auth: Device(DeviceGUID, "key"));

            // user authentication
            var user1 = CreateUser(1);
            var user2 = CreateUser(1, NetworkID);
            Expect(() => Get(resource, auth: user1), FailsWith(404)); // should fail with 404
            Get(resource, auth: user2); // should succeed

            // access key authentication
            var accessKey1 = CreateAccessKey(user1, "GetDeviceNotification");
            var accessKey2 = CreateAccessKey(user2, "GetDeviceNotification", networkIds: new[] { 0 });
            var accessKey3 = CreateAccessKey(user2, "GetDeviceNotification", deviceGuids: new[] { Guid.NewGuid().ToString() });
            var accessKey4 = CreateAccessKey(user2, "GetDeviceNotification");
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
            var resource1 = Create(new { notification = "_ut1" }, auth: user);

            // task to poll new resources
            var poll = new Task(() =>
                {
                    var response = Client.Get(ResourceUri + "/poll?names=_ut1", auth: user);
                    Expect(response.Status, Is.EqualTo(200));
                    Expect(response.Json, Is.InstanceOf<JArray>());

                    var result =  (JArray)response.Json;
                    Expect(result.Count, Is.EqualTo(1));
                    Expect(result[0], Matches(new { notification = "_ut1" }));
                });

            // start poll, wait, then create resources
            poll.Start();
            Thread.Sleep(100);
            var resource2 = Create(new { notification = "_ut2" }, auth: user);
            Thread.Sleep(100);
            var resource3 = Create(new { notification = "_ut1" }, auth: user);

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
                    var response = Client.Get("/device/notification/poll?names=_ut1&deviceGuids=" + DeviceGUID, auth: user);
                    Expect(response.Status, Is.EqualTo(200));
                    Expect(response.Json, Is.InstanceOf<JArray>());

                    var result = (JArray)response.Json;
                    Expect(result.Count, Is.EqualTo(1));
                    Expect(result[0], Matches(new { deviceGuid = DeviceGUID, notification = new { notification = "_ut1" }}));
                });

            // create resource, start poll, wait, then create resources
            var resource1 = Create(new { notification = "_ut1" }, auth: user);
            poll.Start();
            Thread.Sleep(100);
            var resource2 = Create(new { notification = "_ut2" }, auth: user);
            Thread.Sleep(100);
            var resource3 = Create(new { notification = "_ut1" }, auth: user);

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
                    var response = Client.Get("/device/notification/poll", auth: user);
                    Expect(response.Status, Is.EqualTo(200));
                    Expect(response.Json, Is.InstanceOf<JArray>());

                    var result = (JArray)response.Json;
                    Expect(result.Count, Is.EqualTo(1));
                    Expect(result[0], Matches(new { deviceGuid = DeviceGUID, notification = new { notification = "_ut2" } }));
                });

            // start poll, wait, create other response, wait, then create matching resource
            poll.Start();
            Thread.Sleep(100);
            var response1 = Client.Post("/device/" + otherDeviceGuid + "/notification",
                new { notification = "_ut2" }, auth: Device(otherDeviceGuid, "key"));
            Assert.That(response1.Status, Is.EqualTo(ExpectedCreatedStatus));
            Thread.Sleep(100);
            var resource2 = Create(new { notification = "_ut2" }, auth: Device(DeviceGUID, "key"));

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
                    var response = Client.Get("/device/notification/poll?waitTimeout=0", auth: user);
                    Expect(response.Status, Is.EqualTo(200));
                    Expect(response.Json, Is.InstanceOf<JArray>());
                    Expect(response.Json.Count(), Is.EqualTo(0));
                });

            Expect(poll.Wait(2000), Is.True); // task should complete immediately
        }

        [Test]
        public void Create()
        {
            // device authorization
            var resource = Create(new { notification = "_ut" }, auth: Device(DeviceGUID, "key"));
            Expect(Get(resource, auth: Admin), Matches(new { notification = "_ut", parameters = (string)null, timestamp = ResponseMatchesContraint.Timestamp }));

            // user authorization
            var user1 = CreateUser(1);
            var user2 = CreateUser(1, NetworkID);
            Expect(() => Create(new { notification = "_ut" }, auth: user1), FailsWith(404)); // should fail
            Create(new { notification = "_ut" }, auth: user2); // should succeed

            // access keys authorization
            var accessKey1 = CreateAccessKey(user1, "CreateDeviceNotification");
            var accessKey2 = CreateAccessKey(user2, "CreateDeviceNotification", networkIds: new[] { 0 });
            var accessKey3 = CreateAccessKey(user2, "CreateDeviceNotification", deviceGuids: new[] { Guid.NewGuid().ToString() });
            var accessKey4 = CreateAccessKey(user2, "CreateDeviceNotification");
            Expect(() => Create(new { notification = "_ut" }, auth: accessKey1), FailsWith(404)); // should fail with 404
            Expect(() => Create(new { notification = "_ut" }, auth: accessKey2), FailsWith(404)); // should fail with 404
            Expect(() => Create(new { notification = "_ut" }, auth: accessKey3), FailsWith(404)); // should fail with 404
            Create(new { notification = "_ut" }, auth: accessKey4); // should succeed
        }

        [Test]
        public void Update()
        {
            // update is not allwed
            var resource = Create(new { notification = "_ut" }, auth: Device(DeviceGUID, "key"));
            Expect(() => Update(resource, new { notification = "_ut2", parameters = new { a = "b" } }, auth: Admin), FailsWith(405));
        }

        [Test]
        public void Delete()
        {
            // delete is not allwed
            var resource = Create(new { notification = "_ut" }, auth: Device(DeviceGUID, "key"));
            Expect(() => Delete(resource, auth: Admin), FailsWith(405));
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
            Expect(() => List(), FailsWith(401));
            Expect(() => Get(UnexistingResourceID), FailsWith(401));
            Expect(() => Create(new { notification = "_ut" }), FailsWith(401));
            Expect(Client.Get(ResourceUri + "/poll").Status, Is.EqualTo(401));
            Expect(Client.Get("/device/notification/poll").Status, Is.EqualTo(401));

            // device authorization
            Expect(() => List(auth: Device(DeviceGUID, "key")), FailsWith(401));
            Expect(() => Get(UnexistingResourceID, auth: Device(DeviceGUID, "key")), FailsWith(401));
            Expect(Client.Get(ResourceUri + "/poll", auth: Device(DeviceGUID, "key")).Status, Is.EqualTo(401));
            Expect(Client.Get("/device/notification/poll", auth: Device(DeviceGUID, "key")).Status, Is.EqualTo(401));
        }

        [Test]
        public void NotFound()
        {
            Expect(() => Get(UnexistingResourceID, auth: Admin), FailsWith(404));
        }
    }
}
