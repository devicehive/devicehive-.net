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
            // create networks
            var networkResource1 = Create(new { name = "_ut1", key = "_ut_key" }, auth: Admin);
            var networkResource2 = Create(new { name = "_ut2", key = "_ut_key" }, auth: Admin);
            var networkId1 = GetResourceId(networkResource1);
            var networkId2 = GetResourceId(networkResource2);

            // admin: get all networks
            var networks = List(auth: Admin);
            Expect(networks.Any(n => GetResourceId(n) == networkId1), Is.True);
            Expect(networks.Any(n => GetResourceId(n) == networkId2), Is.True);

            // admin: get network by name
            networks = List(new Dictionary<string, string> { { "name", "_ut1" } }, auth: Admin);
            Expect(networks.Count, Is.EqualTo(1));
            Expect(GetResourceId(networks[0]), Is.EqualTo(networkId1));

            // user: get all networks
            var user = CreateUser(1, networkId1);
            networks = List(auth: user);
            Expect(networks.Count, Is.EqualTo(1));
            Expect(GetResourceId(networks[0]), Is.EqualTo(networkId1));

            // accesskey: get all networks
            var accessKey = CreateAccessKey(user, "GetNetwork");
            networks = List(auth: accessKey);
            Expect(networks.Count, Is.EqualTo(1));
            Expect(GetResourceId(networks[0]), Is.EqualTo(networkId1));

            // accesskey: get all networks with no access
            accessKey = CreateAccessKey(user, "GetNetwork", new[] { 0 });
            Expect(List(auth: accessKey).Count, Is.EqualTo(0));
        }

        [Test]
        public void Get()
        {
            // create network
            var networkResource = Create(new { name = "_ut1", key = "_ut_key" }, auth: Admin);
            var networkId = GetResourceId(networkResource);

            // create two users
            var user1 = CreateUser(1);
            var user2 = CreateUser(1, networkId);

            // verify that Get(id) response succeeds only for one network
            Expect(() => Get(networkId, auth: user1), FailsWith(404)); // should fail with 404
            var network = Get(networkId, auth: user2); // should succeed

            // verify that access keys can receive network
            var accessKey1 = CreateAccessKey(user1, "GetNetwork");
            var accessKey2 = CreateAccessKey(user2, "GetNetwork", networkIds: new[] { 0 });
            var accessKey3 = CreateAccessKey(user2, "GetNetwork");
            Expect(() => Get(networkId, auth: accessKey1), FailsWith(404)); // should fail with 404
            Expect(() => Get(networkId, auth: accessKey2), FailsWith(404)); // should fail with 404
            Get(networkId, auth: accessKey3); // should succeed
        }

        [Test]
        public void Create()
        {
            // admin authorization
            var resource = Create(new { name = "_ut", key = "_ut_key" }, auth: Admin);
            Expect(Get(resource, auth: Admin), Matches(new { name = "_ut", key = "_ut_key", description = (string)null }));

            // access key authorization
            var accessKey = CreateAccessKey(Admin, "ManageNetwork");
            resource = Create(new { name = "_ut2" }, auth: accessKey);
            Expect(Get(resource, auth: Admin), Matches(new { name = "_ut2" }));
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
                network = new { name = "_ut" }, deviceClass = new { name = "_ut_dc", version = "1" }}, auth: Admin);
            Expect(deviceResponse.Status, Is.EqualTo(ExpectedUpdatedStatus));
            RegisterForDeletion("/device/" + deviceId);

            // verify that response includes the list of devices
            Expect(Get(resource, auth: Admin), Matches(new { name = "_ut", description = (string)null,
                devices = new[] { new { id = deviceId, name = "_ut_d", status = (string)null,
                    network = new { id = int.Parse(GetResourceId(resource)), name = "_ut" },
                    deviceClass = new { id = deviceClassId, name = "_ut_dc", version = "1" }}}}));

            // verify the devices are filtered according to access key permissions
            var user = CreateUser(1, resource);
            var accessKey1 = CreateAccessKey(user, "GetNetwork");
            var accessKey2 = CreateAccessKey(user, new[] { "GetNetwork", "GetDevice" }, deviceGuids: new[] { Guid.NewGuid().ToString() });
            var accessKey3 = CreateAccessKey(user, new[] { "GetNetwork", "GetDevice" });
            Expect(Get(resource, auth: accessKey1)["devices"].Count(), Is.EqualTo(0));
            Expect(Get(resource, auth: accessKey2)["devices"].Count(), Is.EqualTo(0));
            Expect(Get(resource, auth: accessKey3)["devices"].Count(), Is.EqualTo(1));
        }

        [Test]
        public void Update()
        {
            var resource = Create(new { name = "_ut", key = "_ut_key" }, auth: Admin);

            // admin authorization
            Update(resource, new { name = "_ut2", key = "_ut_key2", description = "desc2" }, auth: Admin);
            Expect(Get(resource, auth: Admin), Matches(new { name = "_ut2", key = "_ut_key2", description = "desc2" }));

            // access key authorization
            var accessKey = CreateAccessKey(Admin, "ManageNetwork");
            Update(resource, new { name = "_ut3", key = "_ut_key3", description = "desc3" }, auth: accessKey);
            Expect(Get(resource, auth: Admin), Matches(new { name = "_ut3", key = "_ut_key3", description = "desc3" }));
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
            // admin authorization
            var resource = Create(new { name = "_ut" }, auth: Admin);
            Delete(resource, auth: Admin);
            Expect(() => Get(resource, auth: Admin), FailsWith(404));

            // access key authorization
            resource = Create(new { name = "_ut" }, auth: Admin);
            var accessKey = CreateAccessKey(Admin, "ManageNetwork");
            Delete(resource, auth: accessKey);
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
            Expect(() => List(), FailsWith(401));
            Expect(() => Get(UnexistingResourceID), FailsWith(401));
            Expect(() => Create(new { name = "_ut" }), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { name = "_ut" }), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID), FailsWith(401));

            // user authorization
            var user = CreateUser(1);
            Expect(() => Create(new { name = "_ut" }, auth: user), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { name = "_ut" }, auth: user), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID, auth: user), FailsWith(401));

            // dummy access key authorization
            var accessKey = CreateAccessKey(Admin, "Dummy");
            Expect(() => List(), FailsWith(401));
            Expect(() => Get(UnexistingResourceID), FailsWith(401));
            Expect(() => Create(new { name = "_ut" }, auth: accessKey), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { name = "_ut" }, auth: accessKey), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID, auth: accessKey), FailsWith(401));

            // access key for non-admin role authorization
            accessKey = CreateAccessKey(user, "ManageNetwork");
            Expect(() => Create(new { name = "_ut" }, auth: accessKey), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { name = "_ut" }, auth: accessKey), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID, auth: accessKey), FailsWith(401));
        }

        [Test]
        public void NotFound()
        {
            Expect(() => Get(UnexistingResourceID, auth: Admin), FailsWith(404));
            Expect(() => Update(UnexistingResourceID, new { name = "_ut" }, auth: Admin), FailsWith(404));
            Delete(UnexistingResourceID, auth: Admin); // should not fail
        }
    }
}
