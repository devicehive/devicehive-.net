using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DeviceHive.Test.ApiTest
{
    [TestFixture]
    public class ApiInfoTest : ResourceTest
    {
        public ApiInfoTest()
            : base("/info")
        {
        }

        [Test]
        public void Get()
        {
            // invoke get
            var response = Client.Get(ResourceUri);

            // verify response object
            Expect(response.Status, Is.EqualTo(200));
            Expect(response.Json, Is.InstanceOf<JObject>());
            Expect((string)response.Json["apiVersion"], Is.Not.Null);
            Expect((DateTime?)response.Json["serverTimestamp"], Is.Not.Null);
        }

        [Test]
        public void GetConfigAuth()
        {
            // invoke get
            var response = Client.Get(ResourceUri + "/config/auth");

            // verify response object
            Expect(response.Status, Is.EqualTo(200));
            Expect(response.Json, Is.InstanceOf<JObject>());
            Expect(response.Json["providers"] as JArray, Is.Not.Null);
        }
    }
}
