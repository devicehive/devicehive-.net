using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Net;
using System.Text;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace DeviceHive.Test.ApiTest
{
    [TestFixture]
    public class OAuthTest : ResourceTest
    {
        private new Authorization User { get; set; }
        private int? ClientID { get; set; }
        private string ClientSecret { get; set; }

        public OAuthTest()
            : base("/oauth2")
        {
        }

        protected override void OnCreateDependencies()
        {
            // create a user
            var userResponse = Client.Post("/user", new { login = "_ut", password = NewUserPassword, role = 1, status = 0 }, auth: Admin);
            Assert.That(userResponse.Status, Is.EqualTo(ExpectedCreatedStatus));
            var userId = (int)userResponse.Json["id"];
            User = new Authorization("User", "_ut", NewUserPassword, userId);
            RegisterForDeletion("/user/" + userId);

            // create a client
            var clientResponse = Client.Post("/oauth/client", new { name = "_ut", oauthId = "_ut_", domain = "_ut.com", redirectUri = "_ut.com", subnet = "127.0.0.0/24" }, auth: Admin);
            Assert.That(clientResponse.Status, Is.EqualTo(ExpectedCreatedStatus));
            ClientID = (int)clientResponse.Json["id"];
            ClientSecret = (string)clientResponse.Json["oauthSecret"];
            RegisterForDeletion("/oauth/client/" + ClientID);
        }

        [Test]
        public void Token_AuthCode()
        {
            // create a grant
            var grantResponse = Client.Post("/user/" + User.ID + "/oauth/grant", new { client = new { oauthId = "_ut_" }, type = "Code", redirectUri = "_ut1.com", scope = "GetNetwork" }, auth: User);
            Assert.That(grantResponse.Status, Is.EqualTo(ExpectedCreatedStatus));
            var grantId = (int)grantResponse.Json["id"];
            RegisterForDeletion("/user/" + User.ID + "/oauth/grant/" + grantId);

            grantResponse = Client.Get("/user/" + User.ID + "/oauth/grant/" + grantId, auth: User);
            var accessKey = grantResponse.Json["accessKey"];

            // exchange code
            var response = SendRequest(new { grant_type = "authorization_code", code = (string)grantResponse.Json["authCode"], redirect_uri = "_ut1.com" },
                auth: "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("{0}:{1}", "_ut_", ClientSecret))));

            // verify token response
            Expect(response.Status, Is.EqualTo(200));
            Expect((string)response.Json["access_token"], Is.EqualTo((string)accessKey["key"]));
            Expect((string)response.Json["token_type"], Is.EqualTo("Bearer"));
            Expect((string)response.Json["expires_in"], Is.Not.Null);

            // verify auth code has been revoked
            var grantResponse2 = Client.Get("/user/" + User.ID + "/oauth/grant/" + grantId, auth: User);
            Expect((string)grantResponse2.Json["authCode"], Is.Null);
        }

        [Test]
        public void Token_Password()
        {
            // exchange password
            var response = SendRequest(new { grant_type = "password", scope = "GetNetwork", username = User.Login, password = User.Password },
                auth: "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("{0}:{1}", "_ut_", ClientSecret))));

            // verify token response
            Expect(response.Status, Is.EqualTo(200));
            Expect((string)response.Json["access_token"], Is.Not.Null);
            Expect((string)response.Json["token_type"], Is.EqualTo("Bearer"));
            Expect((string)response.Json["expires_in"], Is.Not.Null);

            // verify access key has been created
            var grantsResponse = Client.Get("/user/" + User.ID + "/oauth/grant", auth: User);
            Assert.That(grantsResponse.Status, Is.EqualTo(200));
            Assert.That(grantsResponse.Json.Count(), Is.EqualTo(1));
            var grantResponse = grantsResponse.Json[0];
            RegisterForDeletion("/user/" + User.ID + "/oauth/grant/" + (int)grantResponse["id"]);
            
            // verify access key properties
            Expect(grantResponse, Matches(new { timestamp = ResponseMatchesContraint.Timestamp,
                client = new { name = "_ut", oauthId = "_ut_", domain = "_ut.com", redirectUri = "_ut.com" },
                accessKey = new { label = "OAuth token for: _ut" }, type = "Password", accessType = "Online", scope = "GetNetwork" }));
            Expect((string)grantResponse["accessKey"]["key"], Is.EqualTo((string)response.Json["access_token"]));
            Expect((string)grantResponse["accessKey"]["expirationDate"], Is.Not.Null);
            Expect(grantResponse["accessKey"]["permissions"][0], Matches(new { domains = new[] { "_ut.com" },
                subnets = new[] { "127.0.0.0/24" }, actions = new[] { "GetNetwork" } }));
        }

        private JsonResponse SendRequest(object form, string auth = null)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(Client.BaseUrl + "/oauth2/token");
            request.Method = "POST";
            request.Accept = "application/json";
            request.ContentType = "application/x-www-form-urlencoded";
            if (auth != null)
            {
                request.Headers["Authorization"] = auth;
            }
            using (var stream = request.GetRequestStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    var index = 0;
                    foreach (var property in form.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        var value = property.GetValue(form, null).ToString();
                        
                        if (index++ > 0)
                            writer.Write("&");

                        writer.Write(string.Format("{0}={1}", property.Name, Uri.EscapeUriString(value)));
                    }
                }
            }

            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                response = (HttpWebResponse)ex.Response;
            }

            using (var stream = response.GetResponseStream())
            {
                using (var reader = new StreamReader(stream))
                {
                    var responseString = reader.ReadToEnd();
                    try
                    {
                        var json = string.IsNullOrEmpty(responseString) ? null : JToken.Parse(responseString);
                        return new JsonResponse((int)response.StatusCode, json);
                    }
                    catch (JsonReaderException ex)
                    {
                        throw new WebException(string.Format("Error while parsing server response! " +
                            "Status: {0}, Response: {1}", (int)response.StatusCode, responseString), ex);
                    }
                }
            }
        }

    }
}
