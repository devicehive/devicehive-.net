using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DeviceHive.Test.ApiTest
{
    [TestFixture]
    public class UserTest : ResourceTest
    {
        public UserTest()
            : base("/user")
        {
        }

        [Test]
        public void GetAll()
        {
            Get(auth: Admin);
        }

        [Test]
        public void Create()
        {
            var resource = Create(new { login = "_ut", password = "pwd", role = 0, status = 0 }, auth: Admin);

            Expect(resource, Matches(new { login = "_ut", role = 0, status = 0, lastLogin = (DateTime?)null }));
            Expect(Get(resource, auth: Admin), Matches(new { login = "_ut", role = 0, status = 0, lastLogin = (DateTime?)null }));
        }

        [Test]
        public void Create_Existing()
        {
            var resource = Create(new { login = "_ut", password = "pwd", role = 0, status = 0 }, auth: Admin);
            Expect(() => Create(new { login = "_ut", password = "pwd", role = 0, status = 0 }, auth: Admin), FailsWith(403));
        }

        [Test]
        public void Create_UserNetworks()
        {
            // create user
            var resource = Create(new { login = "_ut", password = "pwd", role = 0, status = 0 }, auth: Admin);
            var userId = GetResourceId(resource);

            // create network
            var networkResponse = Client.Post("/network", new { name = "_ut_n" }, auth: Admin);
            Expect(networkResponse.Status, Is.EqualTo(ExpectedCreatedStatus));
            var networkId = (int)((JObject)networkResponse.Json)["id"];
            RegisterForDeletion("/network/" + networkId);

            // create user/network
            var userNetworkResponse = Client.Put("/user/" + userId + "/network/" + networkId, new { }, auth: Admin);
            Expect(userNetworkResponse.Status, Is.EqualTo(200));
            var userNetwork = (JObject)userNetworkResponse.Json;
            RegisterForDeletion("/user/" + userId + "/network/" + networkId);

            // verify that response includes the list of networks
            Expect(Get(resource, auth: Admin), Matches(new { login = "_ut", role = 0, status = 0, lastLogin = (DateTime?)null,
                networks = new[] { new { network = new { id = networkId, name = "_ut_n" }}}}));
        }

        [Test]
        public void Update()
        {
            var resource = Create(new { login = "_ut", password = "pwd", role = 0, status = 0 }, auth: Admin);
            var update = Update(resource, new { login = "_ut2", password = "pwd2", role = 1, status = 1 }, auth: Admin);

            Expect(update, Matches(new { login = "_ut2", role = 1, status = 1, lastLogin = (DateTime?)null }));
            Expect(Get(resource, auth: Admin), Matches(new { login = "_ut2", role = 1, status = 1, lastLogin = (DateTime?)null }));
        }

        [Test]
        public void Update_Partial()
        {
            var resource = Create(new { login = "_ut", password = "pwd", role = 0, status = 0 }, auth: Admin);
            var update = Update(resource, new { status = 1 }, auth: Admin);

            Expect(update, Matches(new { login = "_ut", role = 0, status = 1, lastLogin = (DateTime?)null }));
            Expect(Get(resource, auth: Admin), Matches(new { login = "_ut", role = 0, status = 1, lastLogin = (DateTime?)null }));
        }

        [Test]
        public void Delete()
        {
            var resource = Create(new { login = "_ut", password = "pwd", role = 0, status = 0 }, auth: Admin);
            Delete(resource, auth: Admin);

            Expect(() => Get(resource, auth: Admin), FailsWith(404));
        }

        [Test]
        public void BadRequest()
        {
            Expect(() => Create(new { login = "_ut" }, auth: Admin), FailsWith(400));
        }

        [Test]
        public void Unauthorized()
        {
            // no authorization
            Expect(() => Get(), FailsWith(401));
            Expect(() => Get(UnexistingResourceID), FailsWith(401));
            Expect(() => Create(new { login = "_ut" }), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { login = "_ut" }), FailsWith(401));
            Expect(() => { Delete(UnexistingResourceID); return false; }, FailsWith(401));

            // user authorization
            var user = CreateUser(1);
            Expect(() => Get(auth: user), FailsWith(401));
            Expect(() => Get(UnexistingResourceID, auth: user), FailsWith(401));
            Expect(() => Create(new { login = "_ut" }, auth: user), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { login = "_ut" }, auth: user), FailsWith(401));
            Expect(() => { Delete(UnexistingResourceID, auth: user); return false; }, FailsWith(401));
        }

        [Test]
        public void NotFound()
        {
            Expect(() => Get(UnexistingResourceID, auth: Admin), FailsWith(404));
            Expect(() => Update(UnexistingResourceID, new { login = "_ut" }, auth: Admin), FailsWith(404));
            Delete(UnexistingResourceID, auth: Admin); // should not fail
        }
    }
}
