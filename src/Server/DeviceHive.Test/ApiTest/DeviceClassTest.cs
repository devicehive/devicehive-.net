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
            List(auth: Admin);
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
            var deviceClass = new { name = "_ut", version = "1", offlineTimeout = 3600, equipment = new[] {
                new { name = "_ut_name", type = "_ut_type", code = "_ut_code" } }};

            var resource = Create(deviceClass, auth: Admin);
            Expect(Get(resource, auth: Admin), Matches(deviceClass));
        }

        [Test]
        public void Create_Existing()
        {
            var resource = Create(new { name = "_ut", version = "1" }, auth: Admin);
            Expect(() => Create(new { name = "_ut", version = "1" }, auth: Admin), FailsWith(403));
        }

        [Test]
        public void Update()
        {
            var resource = Create(new { name = "_ut", version = "1", equipment = new[] { new { name = "_ut_name1", type = "_ut_type1", code = "_ut_code1" } } }, auth: Admin);
            Update(resource, new { name = "_ut2", version = "2", isPermanent = true, offlineTimeout = 3600, data = new { a = "b" }, equipment = new[] { new { name = "_ut_name2", type = "_ut_type2", code = "_ut_code2" } } }, auth: Admin);

            Expect(Get(resource, auth: Admin), Matches(new { name = "_ut2", version = "2", isPermanent = true, offlineTimeout = 3600, data = new { a = "b" }, equipment = new[] { new { name = "_ut_name2", type = "_ut_type2", code = "_ut_code2" } } }));
        }

        [Test]
        public void Update_Partial()
        {
            var resource = Create(new { name = "_ut", version = "1" }, auth: Admin);
            Update(resource, new { version = "2" }, auth: Admin);

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
            Expect(() => List(), FailsWith(401));
            Expect(() => Get(UnexistingResourceID), FailsWith(401));
            Expect(() => Create(new { name = "_ut", version = "1" }), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { name = "_ut", version = "1" }), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID), FailsWith(401));

            // user authorization
            var user = CreateUser(1);
            Expect(() => List(auth: user), FailsWith(401));
            Expect(() => Create(new { name = "_ut", version = "1" }, auth: user), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { name = "_ut", version = "1" }, auth: user), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID, auth: user), FailsWith(401));
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
