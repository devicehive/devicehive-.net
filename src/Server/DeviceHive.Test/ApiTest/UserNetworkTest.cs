using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DeviceHive.Test.ApiTest
{
    [TestFixture]
    public class UserNetworkTest : ResourceTest
    {
        private int? NetworkID { get; set; }

        public UserNetworkTest()
            : base("/user/{0}/network")
        {
        }

        protected override void OnCreateDependencies()
        {
            var userResponse = Client.Post("/user", new { login = "_ut", password = NewUserPassword, role = 0, status = 0 }, auth: Admin);
            Assert.That(userResponse.Status, Is.EqualTo(ExpectedCreatedStatus));
            var userId = (int)userResponse.Json["id"];
            RegisterForDeletion("/user/" + userId);
            ResourceUri = "/user/" + userId + "/network";

            var networkResponse = Client.Post("/network", new { name = "_ut_n" }, auth: Admin);
            Assert.That(networkResponse.Status, Is.EqualTo(ExpectedCreatedStatus));
            NetworkID = (int)networkResponse.Json["id"];
            RegisterForDeletion("/network/" + NetworkID);
        }

        [Test]
        public void Create()
        {
            Update(NetworkID, new { }, auth: Admin);
            RegisterForDeletion(ResourceUri + "/" + NetworkID);

            Expect(Get(NetworkID, auth: Admin), Matches(new { network = new { id = NetworkID.Value, name = "_ut_n" } }));
        }

        [Test]
        public void Update()
        {
            Update(NetworkID, new { }, auth: Admin);
            RegisterForDeletion(ResourceUri + "/" + NetworkID);

            Update(NetworkID, new { }, auth: Admin);

            Expect(Get(NetworkID, auth: Admin), Matches(new { network = new { id = NetworkID.Value, name = "_ut_n" } }));
        }

        [Test]
        public void Delete()
        {
            Update(NetworkID, new { }, auth: Admin);
            RegisterForDeletion(ResourceUri + "/" + NetworkID);

            Delete(NetworkID, auth: Admin);

            Expect(() => Get(NetworkID, auth: Admin), FailsWith(404));
        }

        [Test]
        public void Unauthorized()
        {
            // no authorization
            Expect(() => Get(UnexistingResourceID), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { }), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID), FailsWith(401));

            // user authorization
            var user = CreateUser(1);
            Expect(() => Get(UnexistingResourceID, auth: user), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { }, auth: user), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID, auth: user), FailsWith(401));
        }

        [Test]
        public void NotFound()
        {
            Expect(() => Get(UnexistingResourceID, auth: Admin), FailsWith(404));
            Expect(() => Update(UnexistingResourceID, new { }, auth: Admin), FailsWith(404));
            Delete(UnexistingResourceID, auth: Admin); // should not fail
        }

        protected override string GetResourceId(object resource)
        {
            var jObject = resource as JObject;
            if (jObject != null && jObject["network"] != null)
                return jObject["network"]["id"].ToString();

            return base.GetResourceId(resource);
        }
    }
}
