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
            // admin authorization
            List(auth: Admin);

            // access key authorization
            var accessKey = CreateAccessKey(Admin, "ManageDeviceClass");
            List(auth: accessKey);
        }

        [Test]
        public void Get_Client()
        {
            // user authorization
            var user = CreateUser(1);
            var resource = Create(new { name = "_ut", version = "1" }, auth: Admin);
            Expect(Get(resource, auth: user), Matches(new { name = "_ut", version = "1" }));

            // access key authorization
            var accessKey = CreateAccessKey(user, "GetDevice"); // GetDevice allows to retrieve device class by id
            Expect(Get(resource, auth: accessKey), Matches(new { name = "_ut", version = "1" }));
        }

        [Test]
        public void Create()
        {
            // device class creator
            Func<string, object> deviceClass = version => new { name = "_ut", version = version, offlineTimeout = 3600, equipment = new[] {
                new { name = "_ut_name", type = "_ut_type", code = "_ut_code" } }};

            // admin authorization
            var resource = Create(deviceClass("1"), auth: Admin);
            Expect(Get(resource, auth: Admin), Matches(deviceClass("1")));

            // access key authorization
            var accessKey = CreateAccessKey(Admin, "ManageDeviceClass");
            resource = Create(deviceClass("2"), auth: accessKey);
            Expect(Get(resource, auth: Admin), Matches(deviceClass("2")));
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
            
            // admin authorization
            Update(resource, new { name = "_ut2", version = "2", isPermanent = true, offlineTimeout = 3600, data = new { a = "b" }, equipment = new[] { new { name = "_ut_name2", type = "_ut_type2", code = "_ut_code2" } } }, auth: Admin);
            Expect(Get(resource, auth: Admin), Matches(new { name = "_ut2", version = "2", isPermanent = true, offlineTimeout = 3600, data = new { a = "b" }, equipment = new[] { new { name = "_ut_name2", type = "_ut_type2", code = "_ut_code2" } } }));

            // access key authorization
            var accessKey = CreateAccessKey(Admin, "ManageDeviceClass");
            Update(resource, new { name = "_ut3", version = "3", isPermanent = true, offlineTimeout = 3600, data = new { a = "b" }, equipment = new[] { new { name = "_ut_name2", type = "_ut_type2", code = "_ut_code2" } } }, auth: accessKey);
            Expect(Get(resource, auth: Admin), Matches(new { name = "_ut3", version = "3", isPermanent = true, offlineTimeout = 3600, data = new { a = "b" }, equipment = new[] { new { name = "_ut_name2", type = "_ut_type2", code = "_ut_code2" } } }));
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
            // admin authorization
            var resource = Create(new { name = "_ut", version = "1" }, auth: Admin);
            Delete(resource, auth: Admin);
            Expect(() => Get(resource, auth: Admin), FailsWith(404));

            // access key authorization
            var accessKey = CreateAccessKey(Admin, "ManageDeviceClass");
            resource = Create(new { name = "_ut", version = "1" }, auth: Admin);
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
            Expect(() => Create(new { name = "_ut", version = "1" }), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { name = "_ut", version = "1" }), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID), FailsWith(401));

            // user authorization
            var user = CreateUser(1);
            Expect(() => List(auth: user), FailsWith(401));
            Expect(() => Create(new { name = "_ut", version = "1" }, auth: user), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { name = "_ut", version = "1" }, auth: user), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID, auth: user), FailsWith(401));

            // dummy access key authorization
            var accessKey = CreateAccessKey(Admin, "Dummy");
            Expect(() => List(auth: accessKey), FailsWith(401));
            Expect(() => Get(UnexistingResourceID), FailsWith(401));
            Expect(() => Create(new { name = "_ut", version = "1" }, auth: accessKey), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { name = "_ut", version = "1" }, auth: accessKey), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID, auth: accessKey), FailsWith(401));

            // access key for non-admin role authorization
            accessKey = CreateAccessKey(user, "ManageDeviceClass");
            Expect(() => List(auth: accessKey), FailsWith(401));
            Expect(() => Create(new { name = "_ut", version = "1" }, auth: accessKey), FailsWith(401));
            Expect(() => Update(UnexistingResourceID, new { name = "_ut", version = "1" }, auth: accessKey), FailsWith(401));
            Expect(() => Delete(UnexistingResourceID, auth: accessKey), FailsWith(401));
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
