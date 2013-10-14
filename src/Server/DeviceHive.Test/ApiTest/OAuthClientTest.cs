using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DeviceHive.Test.ApiTest
{
    [TestFixture]
    public class OAuthClientTest : ResourceTest
    {
        public OAuthClientTest()
            : base("/oauth/client")
        {
        }

        [Test]
        public void GetAll()
        {
            // create clients
            var clientResource1 = Create(new { name = "_ut1", oauthId = "_ut_1", domain = "_ut1.com", redirectUri = "_ut1.com" }, auth: Admin);
            var clientResource2 = Create(new { name = "_ut2", oauthId = "_ut_2", domain = "_ut2.com", redirectUri = "_ut2.com" }, auth: Admin);
            var clientId1 = GetResourceId(clientResource1);
            var clientId2 = GetResourceId(clientResource2);

            // get all networks
            var clients = List();
            Expect(clients.Any(n => GetResourceId(n) == clientId1), Is.True);
            Expect(clients.Any(n => GetResourceId(n) == clientId2), Is.True);

            // get network by name
            clients = List(new Dictionary<string, string> { { "name", "_ut1" } });
            Expect(clients.Count, Is.EqualTo(1));
            Expect(GetResourceId(clients[0]), Is.EqualTo(clientId1));

            // get network by OAuth ID
            clients = List(new Dictionary<string, string> { { "oauthId", "_ut_1" } });
            Expect(clients.Count, Is.EqualTo(1));
            Expect(GetResourceId(clients[0]), Is.EqualTo(clientId1));

            // get network by domain
            clients = List(new Dictionary<string, string> { { "domain", "_ut1.com" } });
            Expect(clients.Count, Is.EqualTo(1));
            Expect(GetResourceId(clients[0]), Is.EqualTo(clientId1));
        }

        [Test]
        public void Get()
        {
            // create client
            var clientResource = Create(new { name = "_ut", oauthId = "_ut_", domain = "_ut.com", redirectUri = "_ut.com" }, auth: Admin);
            var clientId = GetResourceId(clientResource);

            Get(clientId); // should succeed
        }

        [Test]
        public void Create()
        {
            var resource = Create(new { name = "_ut", oauthId = "_ut_", domain = "_ut.com", redirectUri = "_ut.com" }, auth: Admin);

            Expect(Get(resource), Matches(new { name = "_ut", oauthId = "_ut_", domain = "_ut.com", redirectUri = "_ut.com" }));
        }

        [Test]
        public void Create_Existing()
        {
            var resource = Create(new { name = "_ut", oauthId = "_ut_", domain = "_ut.com", redirectUri = "_ut.com" }, auth: Admin);
            Expect(() => Create(new { name = "_ut2", oauthId = "_ut_", domain = "_ut2.com", redirectUri = "_ut2.com" }, auth: Admin), FailsWith(403));
        }

        [Test]
        public void Update()
        {
            var resource = Create(new { name = "_ut", oauthId = "_ut_", domain = "_ut.com", redirectUri = "_ut.com" }, auth: Admin);
            Update(resource, new { name = "_ut2", oauthId = "_ut_2", domain = "_ut2.com", redirectUri = "_ut2.com" }, auth: Admin);

            Expect(Get(resource), Matches(new { name = "_ut2", oauthId = "_ut_2", domain = "_ut2.com", redirectUri = "_ut2.com" }));
        }

        [Test]
        public void Update_Partial()
        {
            var resource = Create(new { name = "_ut", oauthId = "_ut_", domain = "_ut.com", redirectUri = "_ut.com" }, auth: Admin);
            Update(resource, new { subnet = "127.0.0.0/24" }, auth: Admin);

            Expect(Get(resource), Matches(new { name = "_ut", oauthId = "_ut_", domain = "_ut.com", redirectUri = "_ut.com", subnet = "127.0.0.0/24" }));
        }

        [Test]
        public void Delete()
        {
            var resource = Create(new { name = "_ut", oauthId = "_ut_", domain = "_ut.com", redirectUri = "_ut.com" }, auth: Admin);
            Delete(resource, auth: Admin);

            Expect(() => Get(resource, auth: Admin), FailsWith(404));
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
            Expect(() => Create(new { name = "_ut", oauthId = "_ut_", domain = "_ut.com", redirectUri = "_ut.com" }), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { name = "_ut", oauthId = "_ut_", domain = "_ut.com", redirectUri = "_ut.com" }), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID), FailsWith(401));

            // user authorization
            var user = CreateUser(1);
            Expect(() => Create(new { name = "_ut", oauthId = "_ut_", domain = "_ut.com", redirectUri = "_ut.com" }, auth: user), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { name = "_ut", oauthId = "_ut_", domain = "_ut.com", redirectUri = "_ut.com" }, auth: user), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID, auth: user), FailsWith(401));
        }

        [Test]
        public void NotFound()
        {
            Expect(() => Get(UnexistingResourceID), FailsWith(404));
            Expect(() => Update(UnexistingResourceID, new { name = "_ut" }, auth: Admin), FailsWith(404));
            Delete(UnexistingResourceID, auth: Admin); // should not fail
        }
    }
}
