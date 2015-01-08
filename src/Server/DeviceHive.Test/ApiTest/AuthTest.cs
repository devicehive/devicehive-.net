using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DeviceHive.Test.ApiTest
{
    [TestFixture]
    public class AuthTest : ResourceTest
    {
        private new Authorization User { get; set; }

        public AuthTest()
            : base("/auth")
        {
        }

        protected override void OnCreateDependencies()
        {
            // create a user
            var userResponse = Client.Post("/user", new { login = "_ut", password = NewUserPassword, role = 1, status = 0 }, auth: Admin);
            Assert.That(userResponse.Status, Is.EqualTo(ExpectedCreatedStatus));
            var userId = (int)userResponse.Json["id"];
            User = new Authorization("User", "_ut", NewUserPassword, userId.ToString());
            RegisterForDeletion("/user/" + userId);
        }

        [Test]
        public void AccessKey_Create()
        {
            // issue an access key
            var authResponse = Client.Post("/auth/accesskey", new { providerName = "password", login = User.Login, password = NewUserPassword });
            Assert.That(authResponse.Status, Is.EqualTo(200));
            var authKey = (string)authResponse.Json["key"];
            Assert.That(authKey, Is.Not.Null);

            // verify access key
            var accessKeyResponse = Client.Get("/user/current/accesskey", auth: User);
            Assert.That(accessKeyResponse.Status, Is.EqualTo(200));
            Expect(((JArray)accessKeyResponse.Json).Count, Is.EqualTo(1));
            var accessKey = (JObject)((JArray)accessKeyResponse.Json)[0];
            RegisterForDeletion("/user/" + User.ID + "/accesskey/" + GetResourceId(accessKey));

            Assert.That((string)accessKey["key"], Is.EqualTo(authKey));
            Assert.That((int)accessKey["type"], Is.EqualTo(1)); // session key
            Assert.That((string)accessKey["expirationDate"], Is.Not.Null);
            var accessKeyPermissions = (JArray)accessKey["permissions"];
            Assert.That(accessKeyPermissions.Count, Is.EqualTo(1));
            Assert.That(accessKeyPermissions[0]["actions"].Type, Is.EqualTo(JTokenType.Null));
        }

        [Test]
        public void AccessKey_Delete()
        {
            // issue an access key
            var authResponse = Client.Post("/auth/accesskey", new { providerName = "password", login = User.Login, password = NewUserPassword });
            Assert.That(authResponse.Status, Is.EqualTo(200));
            var authKey = (string)authResponse.Json["key"];
            Assert.That(authKey, Is.Not.Null);

            // delete the access key
            var signoutResponse = Client.Delete("/auth/accesskey", auth: AccessKey(authKey));

            // verify access key was deleted
            var accessKeyResponse = Client.Get("/user/current/accesskey", auth: User);
            Assert.That(accessKeyResponse.Status, Is.EqualTo(200));
            Expect(((JArray)accessKeyResponse.Json).Count, Is.EqualTo(0));
        }

        [Test]
        public void BadRequest()
        {
            Expect(Client.Post("/auth/accesskey", new { }).Status, Is.EqualTo(400)); // missing provider name
        }

        [Test]
        public void Unauthorized()
        {
            // create access key
            Expect(Client.Post("/auth/accesskey", new { providerName = "nonexist" }).Status, Is.EqualTo(401)); // non-supported or disabled provider 
            Expect(Client.Post("/auth/accesskey", new { providerName = "password", login = "xx", password = "xx" }).Status, Is.EqualTo(401));

            // delete access key
            Expect(Client.Delete("/auth/accesskey").Status, Is.EqualTo(401));
        }
    }
}
