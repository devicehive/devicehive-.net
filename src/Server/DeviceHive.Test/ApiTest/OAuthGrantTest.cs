using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DeviceHive.Test.ApiTest
{
    [TestFixture]
    public class OAuthGrantTest : ResourceTest
    {
        private new Authorization User { get; set; }
        private int? ClientID { get; set; }

        public OAuthGrantTest()
            : base("/user/{0}/oauth/grant")
        {
        }

        protected override void OnCreateDependencies()
        {
            var userResponse = Client.Post("/user", new { login = "_ut", password = NewUserPassword, role = 1, status = 0 }, auth: Admin);
            Assert.That(userResponse.Status, Is.EqualTo(ExpectedCreatedStatus));
            var userId = (int)userResponse.Json["id"];
            RegisterForDeletion("/user/" + userId);
            ResourceUri = "/user/" + userId + "/oauth/grant";
            User = new Authorization("User", "_ut", NewUserPassword, userId.ToString());

            var clientResponse = Client.Post("/oauth/client", new { name = "_ut", oauthId = "_ut_", domain = "_ut.com", redirectUri = "_ut.com", subnet = "127.0.0.0/24" }, auth: Admin);
            Assert.That(clientResponse.Status, Is.EqualTo(ExpectedCreatedStatus));
            ClientID = (int)clientResponse.Json["id"];
            RegisterForDeletion("/oauth/client/" + ClientID);
        }

        [Test]
        public void GetAll()
        {
            // create another client
            var clientResponse = Client.Post("/oauth/client", new { name = "_ut2", oauthId = "_ut_2", domain = "_ut2.com", redirectUri = "_ut2.com" }, auth: Admin);
            RegisterForDeletion("/oauth/client/" + (int)clientResponse.Json["id"]);

            // create grants
            var grantResource1 = Create(new { client = new { oauthId = "_ut_" }, type = "Code", accessType = "Offline", redirectUri = "_ut1.com", scope = "GetNetwork", networkIds = new[] { 1, 2 } }, auth: User);
            var grantResource2 = Create(new { client = new { oauthId = "_ut_2" }, type = "Token", accessType = "Online", redirectUri = "_ut2.com", scope = "GetDevice", networkIds = new[] { 2, 3 } }, auth: User);
            var grantId1 = GetResourceId(grantResource1);
            var grantId2 = GetResourceId(grantResource2);

            // get all grants
            var grants = List(auth: User);
            Expect(grants.Any(n => GetResourceId(n) == grantId1), Is.True);
            Expect(grants.Any(n => GetResourceId(n) == grantId2), Is.True);

            // get grant by client OAuth ID
            grants = List(new Dictionary<string, string> { { "clientOAuthId", "_ut_" } }, auth: User);
            Expect(grants.Count, Is.EqualTo(1));
            Expect(GetResourceId(grants[0]), Is.EqualTo(grantId1));

            // get grant by scope
            grants = List(new Dictionary<string, string> { { "scope", "GetDevice" } }, auth: User);
            Expect(grants.Count, Is.EqualTo(1));
            Expect(GetResourceId(grants[0]), Is.EqualTo(grantId2));

            // get grant by redirectUri
            grants = List(new Dictionary<string, string> { { "redirectUri", "_ut2.com" } }, auth: User);
            Expect(grants.Count, Is.EqualTo(1));
            Expect(GetResourceId(grants[0]), Is.EqualTo(grantId2));
        }

        [Test]
        public void Get()
        {
            var grantResource = Create(new { client = new { oauthId = "_ut_" }, type = "Code", redirectUri = "_ut.com", scope = "GetNetwork" }, auth: User);
            var grantId = GetResourceId(grantResource);

            Get(grantId, auth: User); // should succeed
            Get(grantId, auth: Admin); // should also succeed
        }

        [Test]
        public void Create()
        {
            var resource = Create(new { client = new { oauthId = "_ut_" }, type = "Code", redirectUri = "_ut.com", scope = "GetNetwork", networkIds = new[] { 1, 2 } }, auth: User);
            Expect((string)resource["authCode"], Is.Not.Null);  // auth code provided as part of response
            Expect((string)resource["accessKey"], Is.Null);     // access key is not exposed in the Code type

            // verify grant
            var response = Get(resource, auth: User);
            Expect(response, Matches(new { timestamp = ResponseMatchesContraint.Timestamp,
                client = new { name = "_ut", oauthId = "_ut_", domain = "_ut.com", redirectUri = "_ut.com" },
                accessKey = new { label = "OAuth token for: _ut" }, type = "Code", accessType = "Online", redirectUri = "_ut.com", scope = "GetNetwork" }));

            // verify access key
            var accessKey = response["accessKey"];
            Expect((string)accessKey["key"], Is.Not.Null);
            Expect((string)accessKey["expirationDate"], Is.Not.Null);
            var permissions = (JArray)accessKey["permissions"];
            Expect(permissions.Count, Is.EqualTo(1));
            Expect(permissions[0], Matches(new { domains = new[] { "_ut.com" },
                subnets = new[] { "127.0.0.0/24" }, actions = new[] { "GetNetwork" }, networkIds = new[] { 1, 2 } }));
        }

        [Test]
        public void Create_Implicit()
        {
            // creating grant with Token type should return access key
            var resource = Create(new { client = new { oauthId = "_ut_" }, type = "Token", redirectUri = "_ut.com", scope = "GetNetwork", accessType = "Offline" }, auth: User);
            Expect((string)resource["authCode"], Is.Null);      // auth code is not provided as part of response
            Expect(resource["accessKey"], Is.Not.Null); // access key is exposed in the Token type
            Expect((string)resource["accessKey"]["expirationDate"], Is.Null); // should be null for the Offline access type
        }

        [Test]
        public void Update()
        {
            // create the grant
            var response = Create(new { client = new { oauthId = "_ut_" }, type = "Code", redirectUri = "_ut.com", scope = "GetNetwork" }, auth: User);
            var resource = Get(response, auth: User);
            System.Threading.Thread.Sleep(10); // make sure at least one millisecond ticks

            // update the grant
            var response2 = Client.Put(ResourceUri + "/" + GetResourceId(resource), new { scope = "GetDevice", networkIds = new[] { 2, 3 } }, auth: User);
            Expect(response2.Status, Is.EqualTo(200));
            Expect((string)response2.Json["authCode"], Is.Not.Null);  // auth code is provided as part of the update response
            Expect((string)response2.Json["accessKey"], Is.Null);     // access key is not exposed in the Code type

            // check the grant and associated access key were updated
            var resource2 = Get(resource, auth: User);
            Expect(resource2, Matches(new { timestamp = ResponseMatchesContraint.Timestamp, scope = "GetNetwork", networkIds = new[] { 2, 3 } }));
            Expect(resource2["timestamp"].Parent.ToString(), Is.Not.EqualTo(resource["timestamp"].Parent.ToString())); // timestamp must change
            Expect((string)resource2["authCode"], Is.Not.EqualTo((string)resource["authCode"])); // auth code must change
            Expect((string)resource2["accessKey"]["key"], Is.Not.EqualTo((string)resource["accessKey"]["key"])); // access key must change
            Expect((int)resource2["accessKey"]["id"], Is.EqualTo((int)resource["accessKey"]["id"])); // access key id should be preserved
            Expect(resource2["accessKey"]["expirationDate"].Parent.ToString(), Is.Not.EqualTo(resource["accessKey"]["expirationDate"].Parent.ToString()));
            Expect(resource2["accessKey"]["permissions"][0], Matches(new { domains = new[] { "_ut.com" },
                subnets = new[] { "127.0.0.0/24" }, actions = new[] { "GetDevice" }, networkIds = new[] { 2, 3 } }));
        }

        [Test]
        public void Update_Implicit()
        {
            // create the grant
            var resource = Create(new { client = new { oauthId = "_ut_" }, type = "Token", redirectUri = "_ut.com", scope = "GetNetwork", accessType = "Offline" }, auth: User);
            System.Threading.Thread.Sleep(10); // make sure at least one millisecond ticks

            // update the grant
            var response2 = Client.Put(ResourceUri + "/" + GetResourceId(resource), new { scope = "GetDevice", networkIds = new[] { 2, 3 } }, auth: User);
            Expect(response2.Status, Is.EqualTo(200));
            Expect((string)response2.Json["authCode"], Is.Null);      // auth code is not provided
            Expect(response2.Json["accessKey"], Is.Not.Null); // access key is exposed in the Token type
            Expect((string)response2.Json["accessKey"]["key"], Is.Not.EqualTo((string)resource["accessKey"]["key"])); // access key must change
            Expect((int)response2.Json["accessKey"]["id"], Is.EqualTo((int)resource["accessKey"]["id"])); // access key id should be preserved
            
            var resource2 = Get(response2.Json, auth: User);
            Expect((string)resource2["accessKey"]["expirationDate"], Is.Null); // should be null for the Offline access type
            Expect(resource2["accessKey"]["permissions"][0], Matches(new { domains = new[] { "_ut.com" },
                subnets = new[] { "127.0.0.0/24" }, actions = new[] { "GetDevice" }, networkIds = new[] { 2, 3 } }));
        }

        [Test]
        public void Delete()
        {
            var resource = Create(new { client = new { oauthId = "_ut_" }, type = "Token", redirectUri = "_ut.com", scope = "GetNetwork" }, auth: User);
            Delete(resource, auth: User);

            Expect(() => Get(resource, auth: User), FailsWith(404));

            var accessKeyResource = Client.Get("/user/" + User.ID + "/accesskey/" + (int)resource["accessKey"]["id"], auth: User);
            Expect(accessKeyResource.Status, Is.EqualTo(404)); // access key must also delete
        }

        [Test]
        public void BadRequest()
        {
            Expect(() => Create(new { name2 = "_ut" }, auth: User), FailsWith(400));
        }

        [Test]
        public void Unauthorized()
        {
            // no authorization
            Expect(() => List(), FailsWith(401));
            Expect(() => Get(UnexistingResourceID), FailsWith(401));
            Expect(() => Create(new { name = "_ut" }), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { }), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID), FailsWith(401));

            // user authorization
            var user = CreateUser(1);
            Expect(() => List(auth: user), FailsWith(401));
            Expect(() => Get(UnexistingResourceID, auth: user), FailsWith(401));
            Expect(() => Create(new { name = "_ut" }, auth: user), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { }, auth: user), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID, auth: user), FailsWith(401));

            // dummy access key authorization
            var accessKey = CreateAccessKey(Admin, "ManageOAuthGrant"); // ManageUser permission should be required
            Expect(() => List(auth: accessKey), FailsWith(401));
            Expect(() => Get(UnexistingResourceID, auth: accessKey), FailsWith(401));
            Expect(() => Create(new { name = "_ut" }, auth: accessKey), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { }, auth: accessKey), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID, auth: accessKey), FailsWith(401));

            // access key for non-admin role authorization
            accessKey = CreateAccessKey(user, "ManageUser");
            Expect(() => List(auth: accessKey), FailsWith(401));
            Expect(() => Get(UnexistingResourceID, auth: accessKey), FailsWith(401));
            Expect(() => Create(new { name = "_ut" }, auth: accessKey), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { }, auth: accessKey), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID, auth: accessKey), FailsWith(401));
        }

        [Test]
        public void NotFound()
        {
            Expect(() => Get(UnexistingResourceID, auth: Admin), FailsWith(404));
            Expect(() => Update(UnexistingResourceID, new { }, auth: Admin), FailsWith(404));
            Delete(UnexistingResourceID, auth: Admin); // should not fail
        }
    }
}
