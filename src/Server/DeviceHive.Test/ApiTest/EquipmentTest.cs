using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DeviceHive.Test.ApiTest
{
    [TestFixture]
    public class EquipmentTest : ResourceTest
    {
        public EquipmentTest()
            : base("/device/class/{0}/equipment")
        {
        }

        protected override void OnCreateDependencies()
        {
            var deviceClassResponse = Client.Post("/device/class", new { name = "_ut_dc", version = "1" }, auth: Admin);
            Assert.That(deviceClassResponse.Status, Is.EqualTo(ExpectedCreatedStatus));
            var deviceClassId = (int)deviceClassResponse.Json["id"];
            RegisterForDeletion("/device/class/" + deviceClassId);
            ResourceUri = "/device/class/" + deviceClassId + "/equipment";
        }

        [Test]
        public void GetAll()
        {
            Expect(() => Get(auth: Admin), FailsWith(405));
        }

        [Test]
        public void Create()
        {
            var resource = Create(new { name = "_ut", code = "code", type = "type" }, auth: Admin);

            Expect(resource, Matches(new { name = "_ut", code = "code", type = "type" }));
            Expect(Get(resource, auth: Admin), Matches(new { name = "_ut", code = "code", type = "type" }));
        }

        [Test]
        public void Update()
        {
            var resource = Create(new { name = "_ut", code = "code", type = "type" }, auth: Admin);
            var update = Update(resource, new { name = "_ut2", code = "code2", type = "type2" }, auth: Admin);

            Expect(update, Matches(new { name = "_ut2", code = "code2", type = "type2" }));
            Expect(Get(resource, auth: Admin), Matches(new { name = "_ut2", code = "code2", type = "type2" }));
        }

        [Test]
        public void Update_Partial()
        {
            var resource = Create(new { name = "_ut", code = "code", type = "type" }, auth: Admin);
            var update = Update(resource, new { code = "code2" }, auth: Admin);

            Expect(update, Matches(new { name = "_ut", code = "code2", type = "type" }));
            Expect(Get(resource, auth: Admin), Matches(new { name = "_ut", code = "code2", type = "type" }));
        }

        [Test]
        public void Delete()
        {
            var resource = Create(new { name = "_ut", code = "code", type = "type" }, auth: Admin);
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
            Expect(() => Update(UnexistingResourceID, new { name = "_ut" }), FailsWith(401));
            Expect(() => { Delete(UnexistingResourceID); return false; }, FailsWith(401));

            // user authorization
            var user = CreateUser(1);
            Expect(() => Get(), FailsWith(401));
            Expect(() => Get(UnexistingResourceID), FailsWith(401));
            Expect(() => Create(new { name = "_ut" }, auth: user), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { name = "_ut" }, auth: user), FailsWith(401));
            Expect(() => { Delete(UnexistingResourceID, auth: user); return false; }, FailsWith(401));
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
