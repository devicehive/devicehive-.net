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
            var userResponse = Client.Post("/user", new { login = "_ut_u", password = "pwd", role = 1, status = 0 }, auth: Admin);
            Assert.That(userResponse.Status, Is.EqualTo(ExpectedCreatedStatus));
            var userId = (int)userResponse.Json["id"];
            RegisterForDeletion("/user/" + userId);
            ResourceUri = "/user/" + userId + "/accesskey";
            Owner = User("_ut_u", "pwd");
        }

        [Test]
        public void GetAll()
        {
            // administrator access
            Get(auth: Admin);

            // user access
            ResourceUri = "/user/current/accesskey";
            Get(auth: Owner);
        }

        [Test]
        public void Create()
        {
            var key = new { label = "_ut", expirationDate = new DateTime(2015, 1, 1), permissions = new[] {
                new { domains = new[] { "www.example.com" }, networks = new[] { 1, 2 }, actions = new[] { "A", "B", "C" } } }};

            // administrator access
            var resource = Create(key, auth: Admin);
            Expect(Get(resource, auth: Admin), Matches(key));

            // user access
            ResourceUri = "/user/current/accesskey";
            resource = Create(key, auth: Owner);
            Expect(Get(resource, auth: Owner), Matches(key));
        }

        [Test]
        public void Update()
        {
            var resource = Create(new { label = "_ut", permissions = new[] { new { subnets = new[] { "127.0.0.1" } } } }, auth: Admin);

            // administrator access
            var key = new { label = "_ut2", permissions = new[] { new { subnets = new[] { "127.0.0.2" } } } };
            Update(resource, key, auth: Admin);
            Expect(Get(resource, auth: Admin), Matches(key));

            // user access
            ResourceUri = "/user/current/accesskey";
            key = new { label = "_ut3", permissions = new[] { new { subnets = new[] { "127.0.0.3" } } } };
            Update(resource, key, auth: Owner);
            Expect(Get(resource, auth: Owner), Matches(key));

        }

        [Test]
        public void Update_Partial()
        {
            var resource = Create(new { label = "_ut" }, auth: Admin);
            Update(resource, new { expirationDate = new DateTime(2015, 1, 1) }, auth: Admin);

            Expect(Get(resource, auth: Admin), Matches(new { label = "_ut", expirationDate = new DateTime(2015, 1, 1) }));
        }

        [Test]
        public void Delete()
        {
            // administrator access
            var resource = Create(new { label = "_ut" }, auth: Admin);
            Delete(resource, auth: Admin);
            Expect(() => Get(resource, auth: Admin), FailsWith(404));

            // user access
            ResourceUri = "/user/current/accesskey";
            resource = Create(new { label = "_ut" }, auth: Owner);
            Delete(resource, auth: Owner);
            Expect(() => Get(resource, auth: Owner), FailsWith(404));
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
            Expect(() => Get(), FailsWith(401));
            Expect(() => Get(UnexistingResourceID), FailsWith(401));
            Expect(() => Create(new { label = "_ut" }), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { label = "_ut" }), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID), FailsWith(401));

            // another user authorization
            var user = CreateUser(1);
            Expect(() => Get(auth: user), FailsWith(401));
            Expect(() => Get(UnexistingResourceID, auth: user), FailsWith(401));
            Expect(() => Create(new { label = "_ut" }, auth: user), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { label = "_ut" }, auth: user), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID, auth: user), FailsWith(401));
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
