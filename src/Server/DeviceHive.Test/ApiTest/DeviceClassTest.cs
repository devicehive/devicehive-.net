using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DeviceHive.Test.ApiTest
{
    [TestFixture]
    public class DeviceClassTest : ResourceTest
    {
        public DeviceClassTest()
            : base("/device/class")
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
            var user = CreateUser(1);
            var resource = Create(new { name = "_ut", version = "1" }, auth: Admin);
            
            Get(resource, auth: user); // should succeed
        }

        [Test]
        public void Create()
        {
            var resource = Create(new { name = "_ut", version = "1", offlineTimeout = 3600 }, auth: Admin);

            Expect(resource, Matches(new { name = "_ut", version = "1", isPermanent = false, offlineTimeout = 3600 }));
            Expect(Get(resource, auth: Admin), Matches(new { name = "_ut", version = "1", isPermanent = false, offlineTimeout = 3600 }));
        }

        [Test]
        public void Create_Existing()
        {
            var resource = Create(new { name = "_ut", version = "1" }, auth: Admin);
            Expect(() => Create(new { name = "_ut", version = "1" }, auth: Admin), FailsWith(403));
        }

        [Test]
        public void Create_Equipment()
        {
            // create device class
            var resource = Create(new { name = "_ut", version = "1" }, auth: Admin);
            var deviceClassId = GetResourceId(resource);

            // create equipment
            var equipmentResponse = Client.Post("/device/class/" + deviceClassId + "/equipment", new { name = "_ut_eq", code = "code", type = "type" }, auth: Admin);
            Expect(equipmentResponse.Status, Is.EqualTo(ExpectedCreatedStatus));
            var equipmentId = (int)((JObject)equipmentResponse.Json)["id"];
            RegisterForDeletion("/device/class/" + deviceClassId + "/equipment/" + equipmentId);

            // verify that response includes the list of equipments
            Expect(Get(resource, auth: Admin), Matches(new { name = "_ut", version = "1", isPermanent = false,
                equipment = new[] { new { id = equipmentId, name = "_ut_eq", code = "code", type = "type" }}}));
        }

        [Test]
        public void Update()
        {
            var resource = Create(new { name = "_ut", version = "1" }, auth: Admin);
            var update = Update(resource, new { name = "_ut2", version = "2", isPermanent = true, offlineTimeout = 3600 }, auth: Admin);

            Expect(update, Matches(new { name = "_ut2", version = "2", isPermanent = true, offlineTimeout = 3600 }));
            Expect(Get(resource, auth: Admin), Matches(new { name = "_ut2", version = "2", isPermanent = true, offlineTimeout = 3600 }));
        }

        [Test]
        public void Update_Partial()
        {
            var resource = Create(new { name = "_ut", version = "1" }, auth: Admin);
            var update = Update(resource, new { version = "2" }, auth: Admin);

            Expect(update, Matches(new { name = "_ut", version = "2", isPermanent = false }));
            Expect(Get(resource, auth: Admin), Matches(new { name = "_ut", version = "2", isPermanent = false }));
        }

        [Test]
        public void Delete()
        {
            var resource = Create(new { name = "_ut", version = "1" }, auth: Admin);
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
            Expect(() => Create(new { name = "_ut", version = "1" }), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { name = "_ut", version = "1" }), FailsWith(401));
            Expect(() => { Delete(UnexistingResourceID); return false; }, FailsWith(401));

            // user authorization
            var user = CreateUser(1);
            Expect(() => Get(auth: user), FailsWith(401));
            Expect(() => Create(new { name = "_ut", version = "1" }, auth: user), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { name = "_ut", version = "1" }, auth: user), FailsWith(401));
            Expect(() => { Delete(UnexistingResourceID, auth: user); return false; }, FailsWith(401));
        }

        [Test]
        public void NotFound()
        {
            Expect(() => Get(UnexistingResourceID, auth: Admin), FailsWith(404));
            Expect(() => Update(UnexistingResourceID, new { name = "_ut", version = "1" }, auth: Admin), FailsWith(404));
            Delete(UnexistingResourceID, auth: Admin); // should not fail
        }
    }
}
