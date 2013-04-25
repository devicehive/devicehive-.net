using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DeviceHive.Test.ApiTest
{
    [TestFixture]
    public class NetworkTest : ResourceTest
    {
        public NetworkTest()
            : base("/network")
        {
        }

        [Test]
        public void GetAll()
        {
            Get(auth: Admin);
        }

        [Test]
        public void Get_Client()
        {
            // create network
            var networkResource = Create(new { name = "_ut1", key = "_ut_key" }, auth: Admin);
            var networkId = GetResourceId(networkResource);

            // create two users
            var user1 = CreateUser(1);
            var user2 = CreateUser(1, networkId);

            // verify that Get() response includes only one network
            var array1 = Get(auth: user1);
            var array2 = Get(auth: user2);
            Expect(array1.Any(n => GetResourceId(n) == networkId), Is.False);
            Expect(array2.Any(n => GetResourceId(n) == networkId), Is.True);

            // verify that Get(id) response succeeds only for one network
            Expect(() => Get(networkId, auth: user1), FailsWith(404)); // should fail with 404
            var network = Get(networkId, auth: user2); // should succeed

            // verify that Get(id) does not include network key
            Expect(network["key"], Is.Null);
        }

        [Test]
        public void Create()
        {
            var resource = Create(new { name = "_ut", key = "_ut_key" }, auth: Admin);
            
            Expect(Get(resource, auth: Admin), Matches(new { name = "_ut", key = "_ut_key", description = (string)null }));
        }

        [Test]
        public void Create_Existing()
        {
            var resource = Create(new { name = "_ut" }, auth: Admin);
            Expect(() => Create(new { name = "_ut" }, auth: Admin), FailsWith(403));
        }

        [Test]
        public void Create_Devices()
        {
            // create network
            var resource = Create(new { name = "_ut" }, auth: Admin);

            // create device class
            var deviceClassResponse = Client.Post("/device/class", new { name = "_ut_dc", version = "1" }, auth: Admin);
            Expect(deviceClassResponse.Status, Is.EqualTo(ExpectedCreatedStatus));
            var deviceClassId = (int)((JObject)deviceClassResponse.Json)["id"];
            RegisterForDeletion("/device/class/" + deviceClassId);

            // create device
            var deviceId = Guid.NewGuid().ToString();
            var deviceResponse = Client.Put("/device/" + deviceId, new { key = "key", name = "_ut_d",
                network = int.Parse(GetResourceId(resource)), deviceClass = deviceClassId}, auth: Admin);
            Expect(deviceResponse.Status, Is.EqualTo(ExpectedUpdatedStatus));
            RegisterForDeletion("/device/" + deviceId);

            // verify that response includes the list of devices
            Expect(Get(resource, auth: Admin), Matches(new { name = "_ut", description = (string)null,
                devices = new[] { new { id = deviceId, name = "_ut_d", status = (string)null,
                    network = new { id = int.Parse(GetResourceId(resource)), name = "_ut" },
                    deviceClass = new { id = deviceClassId, name = "_ut_dc", version = "1" }}}}));
        }

        [Test]
        public void Update()
        {
            var resource = Create(new { name = "_ut", key = "_ut_key" }, auth: Admin);
            Update(resource, new { name = "_ut2", key = "_ut_key2", description = "desc" }, auth: Admin);

            Expect(Get(resource, auth: Admin), Matches(new { name = "_ut2", key = "_ut_key2", description = "desc" }));
        }

        [Test]
        public void Update_Partial()
        {
            var resource = Create(new { name = "_ut", key = "_ut_key", description = "desc" }, auth: Admin);
            Update(resource, new { description = "desc2" }, auth: Admin);

            Expect(Get(resource, auth: Admin), Matches(new { name = "_ut", key = "_ut_key", description = "desc2" }));
        }

        [Test]
        public void Delete()
        {
            var resource = Create(new { name = "_ut" }, auth: Admin);
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
            Expect(() => Get(), FailsWith(401));
            Expect(() => Get(UnexistingResourceID), FailsWith(401));
            Expect(() => Create(new { name = "_ut" }), FailsWith(401));
            Expect(() => { Update(UnexistingResourceID, new { name = "_ut" }); return false; }, FailsWith(401));
            Expect(() => { Delete(UnexistingResourceID); return false; }, FailsWith(401));

            // user authorization
            var user = CreateUser(1);
            Expect(() => Create(new { name = "_ut" }, auth: user), FailsWith(401));
            Expect(() => { Update(UnexistingResourceID, new { name = "_ut" }, auth: user); return false; }, FailsWith(401));
            Expect(() => { Delete(UnexistingResourceID, auth: user); return false; }, FailsWith(401));
        }

        [Test]
        public void NotFound()
        {
            Expect(() => Get(UnexistingResourceID, auth: Admin), FailsWith(404));
            Expect(() => { Update(UnexistingResourceID, new { name = "_ut" }, auth: Admin); return false; }, FailsWith(404));
            Delete(UnexistingResourceID, auth: Admin); // should not fail
        }
    }
}
