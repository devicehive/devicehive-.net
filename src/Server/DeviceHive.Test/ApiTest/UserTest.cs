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
            // create user
            var resource = Create(new { login = "_ut", password = "pwd", role = 1, status = 0 }, auth: Admin);
            var resourceId = GetResourceId(resource);

            // get all users
            var users = List(auth: Admin);
            Expect(users.Count, Is.GreaterThan(1));
            Expect(users.Count(u => GetResourceId(u) == resourceId), Is.EqualTo(1));

            // get user by login
            users = List(new Dictionary<string, string> { { "login", "_ut" } }, auth: Admin);
            Expect(users.Count, Is.EqualTo(1));
            Expect(GetResourceId(users[0]), Is.EqualTo(resourceId));

            // get non-existing user by login
            users = List(new Dictionary<string, string> { { "login", "nonexist" } }, auth: Admin);
            Expect(users.Count, Is.EqualTo(0));
        }

        [Test]
        public void Get_Current()
        {
            // create user
            var resource = Create(new { login = "_ut", password = "pwd", role = 1, status = 0 }, auth: Admin);

            // get current user
            var current = Client.Get(ResourceUri + "/current", auth: User("_ut", "pwd"));
            Expect(current.Status, Is.EqualTo(200));
            Expect(current.Json, Is.InstanceOf<JObject>());
            Expect(current.Json, Matches(new { id = (int)resource["id"], login = "_ut", role = 1, status = 0 }));
        }

        [Test]
        public void Create()
        {
            // create user
            var resource = Create(new { login = "_ut", password = "pwd", role = 0, status = 0 }, auth: Admin);

            // verify server response
            Expect(Get(resource, auth: Admin), Matches(new { login = "_ut", role = 0, status = 0, lastLogin = (DateTime?)null }));
        }

        [Test]
        public void Create_Existing()
        {
            // create user with an existing login
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
            Expect(userNetworkResponse.Status, Is.EqualTo(ExpectedUpdatedStatus));
            var userNetwork = (JObject)userNetworkResponse.Json;
            RegisterForDeletion("/user/" + userId + "/network/" + networkId);

            // verify that response includes the list of networks
            Expect(Get(resource, auth: Admin), Matches(new { login = "_ut", role = 0, status = 0, lastLogin = (DateTime?)null,
                networks = new[] { new { network = new { id = networkId, name = "_ut_n" }}}}));
        }

        [Test]
        public void Update()
        {
            // create and update user
            var resource = Create(new { login = "_ut", password = "pwd", role = 0, status = 0 }, auth: Admin);
            Update(resource, new { login = "_ut2", password = "pwd2", role = 1, status = 1 }, auth: Admin);

            // verify server response
            Expect(Get(resource, auth: Admin), Matches(new { login = "_ut2", role = 1, status = 1, lastLogin = (DateTime?)null }));
        }

        [Test]
        public void Update_Partial()
        {
            // create and update user
            var resource = Create(new { login = "_ut", password = "pwd", role = 0, status = 0 }, auth: Admin);
            Update(resource, new { status = 1 }, auth: Admin);

            // verify server response
            Expect(Get(resource, auth: Admin), Matches(new { login = "_ut", role = 0, status = 1, lastLogin = (DateTime?)null }));
        }

        [Test]
        public void Update_Current()
        {
            // create user
            var resource = Create(new { login = "_ut", password = "pwd", role = 1, status = 0 }, auth: Admin);

            // update user password
            var current = Client.Put(ResourceUri + "/current", new { password = "pwd2" }, auth: User("_ut", "pwd"));
            Expect(current.Status, Is.EqualTo(ExpectedUpdatedStatus));

            // verify user password has been changed
            Expect(Client.Get(ResourceUri + "/current", auth: User("_ut", "pwd")).Status, Is.EqualTo(401));
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
            Expect(() => List(), FailsWith(401));
            Expect(() => Get(UnexistingResourceID), FailsWith(401));
            Expect(() => Get("current"), FailsWith(401));
            Expect(() => Create(new { login = "_ut" }), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { login = "_ut" }), FailsWith(401));
            Expect(() => Update("current", new { }), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID), FailsWith(401));

            // user authorization
            var user = CreateUser(1);
            Expect(() => List(auth: user), FailsWith(401));
            Expect(() => Get(UnexistingResourceID, auth: user), FailsWith(401));
            Expect(() => Create(new { login = "_ut" }, auth: user), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { login = "_ut" }, auth: user), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID, auth: user), FailsWith(401));
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
