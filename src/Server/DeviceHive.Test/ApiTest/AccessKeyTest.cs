using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DeviceHive.Test.ApiTest
{
    [TestFixture]
    public class AccessKeyTest : ResourceTest
    {
        private Authorization Owner { get; set; }

        public AccessKeyTest()
            : base("/user/{0}/accesskey")
        {
        }

        protected override void OnCreateDependencies()
        {
            var userResponse = Client.Post("/user", new { login = "_ut_u", password = NewUserPassword, role = 1, status = 0 }, auth: Admin);
            Assert.That(userResponse.Status, Is.EqualTo(ExpectedCreatedStatus));
            var userId = (int)userResponse.Json["id"];
            RegisterForDeletion("/user/" + userId);
            ResourceUri = "/user/" + userId + "/accesskey";
            Owner = User("_ut_u", NewUserPassword);
        }

        [Test]
        public void GetAll()
        {
            var resource1 = Create(new { type = 0, label = "_ut1" }, auth: Admin);
            var resource2 = Create(new { type = 1, label = "_ut2" }, auth: Admin);
            var accessKeyId1 = GetResourceId(resource1);
            var accessKeyId2 = GetResourceId(resource2);

            // administrator access: list all keys
            var accessKeys = List(auth: Admin);
            Expect(accessKeys.Any(n => GetResourceId(n) == accessKeyId1), Is.True);
            Expect(accessKeys.Any(n => GetResourceId(n) == accessKeyId2), Is.True);

            // administrator access: filter by type
            accessKeys = List(new Dictionary<string, string> { { "type", "1" } }, auth: Admin);
            Expect(accessKeys.Any(n => GetResourceId(n) == accessKeyId1), Is.False);
            Expect(accessKeys.Any(n => GetResourceId(n) == accessKeyId2), Is.True);

            // administrator access: filter by label
            accessKeys = List(new Dictionary<string, string> { { "label", "_ut1" } }, auth: Admin);
            Expect(accessKeys.Any(n => GetResourceId(n) == accessKeyId1), Is.True);
            Expect(accessKeys.Any(n => GetResourceId(n) == accessKeyId2), Is.False);

            // administrator access: filter by labelPattern
            accessKeys = List(new Dictionary<string, string> { { "labelPattern", "t2" } }, auth: Admin);
            Expect(accessKeys.Any(n => GetResourceId(n) == accessKeyId1), Is.False);
            Expect(accessKeys.Any(n => GetResourceId(n) == accessKeyId2), Is.True);

            // administrator access key access
            var accessKey = CreateAccessKey(Admin, "ManageUser");
            List(auth: accessKey);

            // user access
            ResourceUri = "/user/current/accesskey";
            List(auth: Owner);

            // user access key access
            accessKey = CreateAccessKey(Owner, "ManageAccessKey");
            List(auth: accessKey);
        }

        [Test]
        public void Create()
        {
            // key creator
            Func<string, object> key = label => new { type = 0, label = label, expirationDate = new DateTime(2015, 1, 1), permissions = new[] {
                new { domains = new[] { "www.example.com" }, networkIds = new[] { 1, 2 }, actions = new[] { "GetNetwork", "GetDevice" } } }};

            // administrator access
            var resource = Create(key("_ut1"), auth: Admin);
            Expect(Get(resource, auth: Admin), Matches(key("_ut1")));

            // administrator access key access
            var accessKey = CreateAccessKey(Admin, "ManageUser");
            resource = Create(key("_ut2"), auth: accessKey);
            Expect(Get(resource, auth: Admin), Matches(key("_ut2")));

            // user access
            ResourceUri = "/user/current/accesskey";
            resource = Create(key("_ut3"), auth: Owner);
            Expect(Get(resource, auth: Owner), Matches(key("_ut3")));

            // user access key access
            accessKey = CreateAccessKey(Owner, "ManageAccessKey");
            resource = Create(key("_ut4"), auth: accessKey);
            Expect(Get(resource, auth: Owner), Matches(key("_ut4")));
        }

        [Test]
        public void Update()
        {
            var resource = Create(new { type = 0, label = "_ut", permissions = new[] { new { actions = new[] { "GetNetwork" }, subnets = new[] { "127.0.0.1" } } } }, auth: Admin);
            Func<string, object> key = label => new { type = 1, label = label, permissions = new[] { new { actions = new[] { "GetNetwork" }, subnets = new[] { "127.0.0.2" } } } };

            // administrator access
            Update(resource, key("_ut2"), auth: Admin);
            Expect(Get(resource, auth: Admin), Matches(key("_ut2")));

            // administrator access key access
            var accessKey = CreateAccessKey(Admin, "ManageUser");
            resource = Create(key("_ut3"), auth: accessKey);
            Expect(Get(resource, auth: Admin), Matches(key("_ut3")));

            // user access
            ResourceUri = "/user/current/accesskey";
            Update(resource, key("_ut4"), auth: Owner);
            Expect(Get(resource, auth: Owner), Matches(key("_ut4")));

            // user access key access
            accessKey = CreateAccessKey(Owner, "ManageAccessKey");
            Update(resource, key("_ut5"), auth: accessKey);
            Expect(Get(resource, auth: Owner), Matches(key("_ut5")));
        }

        [Test]
        public void Update_Partial()
        {
            var resource = Create(new { label = "_ut", permissions = new[] { new { actions = new[] { "GetNetwork" } } } }, auth: Admin);
            Update(resource, new { expirationDate = new DateTime(2015, 1, 1) }, auth: Admin);

            Expect(Get(resource, auth: Admin), Matches(new { label = "_ut", expirationDate = new DateTime(2015, 1, 1) }));
        }

        [Test]
        public void Delete()
        {
            // administrator access
            var resource = Create(new { label = "_ut", permissions = new[] { new { actions = new[] { "GetNetwork" } } } }, auth: Admin);
            Delete(resource, auth: Admin);
            Expect(() => Get(resource, auth: Admin), FailsWith(404));

            // administrator access key access
            var accessKey = CreateAccessKey(Admin, "ManageUser");
            resource = Create(new { label = "_ut", permissions = new[] { new { actions = new[] { "GetNetwork" } } } }, auth: Admin);
            Delete(resource, auth: accessKey);
            Expect(() => Get(resource, auth: Admin), FailsWith(404));

            // user access
            ResourceUri = "/user/current/accesskey";
            resource = Create(new { label = "_ut", permissions = new[] { new { actions = new[] { "GetNetwork" } } } }, auth: Owner);
            Delete(resource, auth: Owner);
            Expect(() => Get(resource, auth: Owner), FailsWith(404));

            // user access key access
            accessKey = CreateAccessKey(Owner, "ManageAccessKey");
            resource = Create(new { label = "_ut", permissions = new[] { new { actions = new[] { "GetNetwork" } } } }, auth: Owner);
            Delete(resource, auth: accessKey);
            Expect(() => Get(resource, auth: Owner), FailsWith(404));
        }

        [Test]
        public void Authorization()
        {
            // test access key authorization on the network resource
            var networkResponse = Client.Post("/network", new { name = "_ut_n" }, auth: Admin);
            Assert.That(networkResponse.Status, Is.EqualTo(ExpectedCreatedStatus));
            var networkId = (int)networkResponse.Json["id"];
            RegisterForDeletion("/network/" + networkId);

            // create a user
            var user = CreateUser(1, networkId);
            ResourceUri = "/user/current/accesskey";

            // check the key authorization works
            var key = Create(new { label = "_ut", permissions = new[] { new { actions = new[] { "GetNetwork" } } }}, auth: user);
            Expect(Client.Get("/network/" + networkId, auth: AccessKey((string)key["key"])).Status, Is.EqualTo(200));

            // check the key authorization with explicit network works
            key = Create(new { label = "_ut", permissions = new[] { new { networkIds = new[] { networkId }, actions = new[] { "GetNetwork" } } } }, auth: user);
            Expect(Client.Get("/network/" + networkId, auth: AccessKey((string)key["key"])).Status, Is.EqualTo(200));

            // check the key authorization with explicit subnet works
            key = Create(new { label = "_ut", permissions = new[] { new { subnets = new[] { "0.0.0.0/0" }, actions = new[] { "GetNetwork" } } } }, auth: user);
            Expect(Client.Get("/network/" + networkId, auth: AccessKey((string)key["key"])).Status, Is.EqualTo(200));

            // check the expiration date is validated
            key = Create(new { label = "_ut", expirationDate = DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ss.ffffff"),
                permissions = new[] { new { networks = new[] { networkId }, actions = new[] { "GetNetwork" } } } }, auth: user);
            Expect(Client.Get("/network/" + networkId, auth: AccessKey((string)key["key"])).Status, Is.EqualTo(401));

            // check the source subnet is validated
            key = Create(new { label = "_ut", permissions = new[] { new { subnets = new[] { "10.10.10.0/24" }, actions = new[] { "GetNetwork" } } } }, auth: user);
            Expect(Client.Get("/network/" + networkId, auth: AccessKey((string)key["key"])).Status, Is.EqualTo(401));

            // check the action is validated
            key = Create(new { label = "_ut", permissions = new[] { new { actions = new[] { "UpdateDeviceCommand" } } } }, auth: user);
            Expect(Client.Get("/network/" + networkId, auth: AccessKey((string)key["key"])).Status, Is.EqualTo(401));

            // check the network is validated
            key = Create(new { label = "_ut", permissions = new[] { new { networkIds = new[] { networkId + 1 }, actions = new[] { "GetNetwork" } } } }, auth: user);
            Expect(Client.Get("/network/" + networkId, auth: AccessKey((string)key["key"])).Status, Is.EqualTo(404));

            // check the network is validated on admin key
            key = Create(new { label = "_ut", permissions = new[] { new { networkIds = new[] { networkId + 1 }, actions = new[] { "GetNetwork" } } } }, auth: Admin);
            Expect(Client.Get("/network/" + networkId, auth: AccessKey((string)key["key"])).Status, Is.EqualTo(404));
        }

        [Test]
        public void BadRequest()
        {
            Expect(() => Create(new { name2 = "_ut" }, auth: Admin), FailsWith(400));
        }

        [Test]
        public void Unauthorized()
        {
            // no authorization
            Expect(() => List(), FailsWith(401));
            Expect(() => Get(UnexistingResourceID), FailsWith(401));
            Expect(() => Create(new { label = "_ut" }), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { label = "_ut" }), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID), FailsWith(401));

            // another user authorization
            var user = CreateUser(1);
            Expect(() => List(auth: user), FailsWith(401));
            Expect(() => Get(UnexistingResourceID, auth: user), FailsWith(401));
            Expect(() => Create(new { label = "_ut" }, auth: user), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { label = "_ut" }, auth: user), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID, auth: user), FailsWith(401));

            // dummy access key authorization
            var accessKey = CreateAccessKey(Admin, "ManageAccessKey"); // ManageUser permission should be required
            Expect(() => List(auth: accessKey), FailsWith(401));
            Expect(() => Get(UnexistingResourceID, auth: accessKey), FailsWith(401));
            Expect(() => Create(new { label = "_ut" }, auth: accessKey), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { label = "_ut" }, auth: accessKey), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID, auth: accessKey), FailsWith(401));

            // access key for non-admin role authorization
            accessKey = CreateAccessKey(user, "ManageUser");
            Expect(() => List(auth: accessKey), FailsWith(401));
            Expect(() => Get(UnexistingResourceID, auth: accessKey), FailsWith(401));
            Expect(() => Create(new { label = "_ut" }, auth: accessKey), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { label = "_ut" }, auth: accessKey), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID, auth: accessKey), FailsWith(401));
        }

        [Test]
        public void NotFound()
        {
            Expect(() => Get(UnexistingResourceID, auth: Admin), FailsWith(404));
            Expect(() => Update(UnexistingResourceID, new { label = "_ut" }, auth: Admin), FailsWith(404));
            Delete(UnexistingResourceID, auth: Admin); // should not fail
        }
    }
}
