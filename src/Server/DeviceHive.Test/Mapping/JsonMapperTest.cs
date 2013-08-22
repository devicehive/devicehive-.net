using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DeviceHive.Core.Mapping;
using DeviceHive.Data;
using DeviceHive.Data.EF;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeviceHive.Test.Mapping
{
    [TestFixture]
    public class JsonMapperTest
    {
        protected DataContext DataContext { get; private set; }
        protected JsonMapperManager JsonMapperManager { get; private set; }

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            JsonMapperManager = new JsonMapperManager();

            // create and configure mappers
            JsonMapperManager.AddMapper(new JsonMapper<TestInnerClass>(
                new JsonMapperConfiguration<TestInnerClass>(JsonMapperManager)
                    .Property(e => e.Int, "int")
                ));

            JsonMapperManager.AddMapper(new JsonMapper<TestClass>(
                new JsonMapperConfiguration<TestClass>(JsonMapperManager)
                    .Property(e => e.Int, "int")
                    .Property(e => e.IntNullable, "intNullable")
                    .Property(e => e.IntNullable2, "intNullable2")
                    .Property(e => e.Bool, "bool")
                    .Property(e => e.BoolNullable, "boolNullable")
                    .Property(e => e.BoolNullable2, "boolNullable2")
                    .Property(e => e.DateTime, "datetime")
                    .Property(e => e.DateTimeNullable, "datetimeNullable")
                    .Property(e => e.DateTimeNullable2, "datetimeNullable2")
                    .Property(e => e.Guid, "guid")
                    .Property(e => e.GuidNullable, "guidNullable")
                    .Property(e => e.GuidNullable2, "guidNullable2")
                    .Property(e => e.Enum, "enum")
                    .Property(e => e.EnumNullable, "enumNullable")
                    .Property(e => e.EnumNullable2, "enumNullable2")
                    .Property(e => e.String, "string")
                    .Property(e => e.String2, "string2")
                    .Property(e => e.IntArray, "intArray")
                    .Property(e => e.StringArray, "stringArray")
                    .RawJsonProperty(e => e.RawJson, "rawJson")
                    .ReferenceProperty(e => e.InnerReference, "innerReference")
                    .CollectionProperty(e => e.InnerCollection, "innerCollection")
                ));
        }

        [Test]
        public void Property()
        {
            var mapper = JsonMapperManager.GetMapper<TestClass>();

            // map to JSON
            var entity = new TestClass
                {
                    Int = 5,
                    IntNullable = 5,
                    Bool = true,
                    BoolNullable = true,
                    DateTime = DateTime.UtcNow,
                    DateTimeNullable = DateTime.UtcNow,
                    Guid = Guid.NewGuid(),
                    GuidNullable = Guid.NewGuid(),
                    Enum = TestEnum.Value1,
                    EnumNullable = TestEnum.Value1,
                    String = "String",
                    IntArray = new[] { 1, 2, 3 },
                    StringArray = new[] { "a", "b", "c" },
                };
            var json = mapper.Map(entity);

            Assert.That(json.Property("int"), Is.Not.Null);
            Assert.That(json["int"].Type, Is.EqualTo(JTokenType.Integer));
            Assert.That((int)json["int"], Is.EqualTo(entity.Int));

            Assert.That(json.Property("intNullable"), Is.Not.Null);
            Assert.That(json["intNullable"].Type, Is.EqualTo(JTokenType.Integer));
            Assert.That((int)json["intNullable"], Is.EqualTo(entity.IntNullable));

            Assert.That(json.Property("intNullable2"), Is.Not.Null);
            Assert.That(json["intNullable2"].Type, Is.EqualTo(JTokenType.Null));

            Assert.That(json.Property("bool"), Is.Not.Null);
            Assert.That(json["bool"].Type, Is.EqualTo(JTokenType.Boolean));
            Assert.That((bool)json["bool"], Is.EqualTo(entity.Bool));

            Assert.That(json.Property("boolNullable"), Is.Not.Null);
            Assert.That(json["boolNullable"].Type, Is.EqualTo(JTokenType.Boolean));
            Assert.That((bool)json["boolNullable"], Is.EqualTo(entity.BoolNullable));

            Assert.That(json.Property("boolNullable2"), Is.Not.Null);
            Assert.That(json["boolNullable2"].Type, Is.EqualTo(JTokenType.Null));

            Assert.That(json.Property("datetime"), Is.Not.Null);
            Assert.That(json["datetime"].Type, Is.EqualTo(JTokenType.Date));
            Assert.That((DateTime)json["datetime"], Is.EqualTo(entity.DateTime));

            Assert.That(json.Property("datetimeNullable"), Is.Not.Null);
            Assert.That(json["datetimeNullable"].Type, Is.EqualTo(JTokenType.Date));
            Assert.That((DateTime)json["datetimeNullable"], Is.EqualTo(entity.DateTimeNullable.Value));

            Assert.That(json.Property("datetimeNullable2"), Is.Not.Null);
            Assert.That(json["datetimeNullable2"].Type, Is.EqualTo(JTokenType.Null));

            Assert.That(json.Property("guid"), Is.Not.Null);
            Assert.That(json["guid"].Type, Is.EqualTo(JTokenType.String));
            Assert.That((string)json["guid"], Is.EqualTo(entity.Guid.ToString()));

            Assert.That(json.Property("guidNullable"), Is.Not.Null);
            Assert.That(json["guidNullable"].Type, Is.EqualTo(JTokenType.String));
            Assert.That((string)json["guidNullable"], Is.EqualTo(entity.GuidNullable.Value.ToString()));

            Assert.That(json.Property("guidNullable2"), Is.Not.Null);
            Assert.That(json["guidNullable2"].Type, Is.EqualTo(JTokenType.Null));

            Assert.That(json.Property("enum"), Is.Not.Null);
            Assert.That(json["enum"].Type, Is.EqualTo(JTokenType.Integer));
            Assert.That((int)json["enum"], Is.EqualTo((int)entity.Enum));

            Assert.That(json.Property("enumNullable"), Is.Not.Null);
            Assert.That(json["enumNullable"].Type, Is.EqualTo(JTokenType.Integer));
            Assert.That((int)json["enumNullable"], Is.EqualTo((int)entity.EnumNullable));

            Assert.That(json.Property("enumNullable2"), Is.Not.Null);
            Assert.That(json["enumNullable2"].Type, Is.EqualTo(JTokenType.Null));

            Assert.That(json.Property("string"), Is.Not.Null);
            Assert.That(json["string"].Type, Is.EqualTo(JTokenType.String));
            Assert.That((string)json["string"], Is.EqualTo(entity.String));

            Assert.That(json.Property("string2"), Is.Not.Null);
            Assert.That(json["string2"].Type, Is.EqualTo(JTokenType.Null));

            Assert.That(json.Property("intArray"), Is.Not.Null);
            Assert.That(json["intArray"].Type, Is.EqualTo(JTokenType.Array));
            Assert.That(json["intArray"].Select(t => (int)t).ToArray(), Is.EqualTo(entity.IntArray));

            Assert.That(json.Property("stringArray"), Is.Not.Null);
            Assert.That(json["stringArray"].Type, Is.EqualTo(JTokenType.Array));
            Assert.That(json["stringArray"].Select(t => (string)t).ToArray(), Is.EqualTo(entity.StringArray));

            // map to entity
            var json2 = JObject.Parse(json.ToString());
            var entity2 = mapper.Map(json2);

            Assert.That(entity2.Int, Is.EqualTo(entity.Int));
            Assert.That(entity2.IntNullable, Is.EqualTo(entity.IntNullable));
            Assert.That(entity2.IntNullable2, Is.Null);
            Assert.That(entity2.Bool, Is.EqualTo(entity.Bool));
            Assert.That(entity2.BoolNullable, Is.EqualTo(entity.BoolNullable));
            Assert.That(entity2.BoolNullable2, Is.Null);
            Assert.That(entity2.DateTime, Is.EqualTo(entity.DateTime));
            Assert.That(entity2.DateTimeNullable, Is.EqualTo(entity.DateTimeNullable));
            Assert.That(entity2.DateTimeNullable2, Is.Null);
            Assert.That(entity2.Guid, Is.EqualTo(entity.Guid));
            Assert.That(entity2.GuidNullable, Is.EqualTo(entity.GuidNullable));
            Assert.That(entity2.GuidNullable2, Is.Null);
            Assert.That(entity2.Enum, Is.EqualTo(entity.Enum));
            Assert.That(entity2.EnumNullable, Is.EqualTo(entity.EnumNullable));
            Assert.That(entity2.EnumNullable2, Is.Null);
            Assert.That(entity2.String, Is.EqualTo(entity.String));
            Assert.That(entity2.String2, Is.Null);
            Assert.That(entity2.IntArray, Is.EqualTo(entity.IntArray));
            Assert.That(entity2.StringArray, Is.EqualTo(entity.StringArray));

            // map to entity - null values
            var entity3 = new TestClass
                {
                    IntNullable = 5,
                    IntNullable2 = 5,
                    BoolNullable = true,
                    BoolNullable2 = true,
                    DateTimeNullable = DateTime.UtcNow,
                    DateTimeNullable2 = DateTime.UtcNow,
                    GuidNullable = Guid.NewGuid(),
                    GuidNullable2 = Guid.NewGuid(),
                    EnumNullable = TestEnum.Value1,
                    EnumNullable2 = TestEnum.Value1,
                    String = "String",
                    String2 = "String",
                    IntArray = new int[] { 1, 2, 3 },
                    StringArray = new string[] { "a", "b", "c" },
                };
            var json3 = new JObject(
                new JProperty("intNullable2", null),
                new JProperty("boolNullable2", null),
                new JProperty("datetimeNullable2", null),
                new JProperty("guidNullable2", null),
                new JProperty("enumNullable2", null),
                new JProperty("string2", null),
                new JProperty("stringArray", null));
            mapper.Apply(entity3, json3);

            Assert.That(entity3.IntNullable, Is.Not.Null); // should not override
            Assert.That(entity3.IntNullable2, Is.Null);    // should override to null
            Assert.That(entity3.BoolNullable, Is.Not.Null);
            Assert.That(entity3.BoolNullable2, Is.Null);
            Assert.That(entity3.DateTimeNullable, Is.Not.Null);
            Assert.That(entity3.DateTimeNullable2, Is.Null);
            Assert.That(entity3.GuidNullable, Is.Not.Null);
            Assert.That(entity3.GuidNullable2, Is.Null);
            Assert.That(entity3.EnumNullable, Is.Not.Null);
            Assert.That(entity3.EnumNullable2, Is.Null);
            Assert.That(entity3.String, Is.Not.Null);
            Assert.That(entity3.String2, Is.Null);
            Assert.That(entity3.IntArray, Is.Not.Null);
            Assert.That(entity3.StringArray, Is.Null);
        }

        [Test]
        public void RawJsonProperty()
        {
            var mapper = JsonMapperManager.GetMapper<TestClass>();

            // map to JSON
            var entity = new TestClass { RawJson = null };
            var json = mapper.Map(entity);
            Assert.That(json.Property("rawJson"), Is.Not.Null);
            Assert.That(json["rawJson"].Type, Is.EqualTo(JTokenType.Null));

            entity = new TestClass { RawJson = "null" };
            json = mapper.Map(entity);
            Assert.That(json.Property("rawJson"), Is.Not.Null);
            Assert.That(json["rawJson"].Type, Is.EqualTo(JTokenType.Null));

            entity = new TestClass { RawJson = "5" };
            json = mapper.Map(entity);
            Assert.That(json.Property("rawJson"), Is.Not.Null);
            Assert.That(json["rawJson"].Type, Is.EqualTo(JTokenType.Integer));
            Assert.That((int)json["rawJson"], Is.EqualTo(5));

            entity = new TestClass { RawJson = "true" };
            json = mapper.Map(entity);
            Assert.That(json.Property("rawJson"), Is.Not.Null);
            Assert.That(json["rawJson"].Type, Is.EqualTo(JTokenType.Boolean));
            Assert.That((bool)json["rawJson"], Is.EqualTo(true));

            entity = new TestClass { RawJson = "\"String\"" };
            json = mapper.Map(entity);
            Assert.That(json.Property("rawJson"), Is.Not.Null);
            Assert.That(json["rawJson"].Type, Is.EqualTo(JTokenType.String));
            Assert.That((string)json["rawJson"], Is.EqualTo("String"));

            entity = new TestClass { RawJson = "[1, 2, 3]" };
            json = mapper.Map(entity);
            Assert.That(json.Property("rawJson"), Is.Not.Null);
            Assert.That(json["rawJson"].Type, Is.EqualTo(JTokenType.Array));
            Assert.That(json["rawJson"].ToString(Formatting.None), Is.EqualTo("[1,2,3]"));

            entity = new TestClass { RawJson = "{\"a\": 5}" };
            json = mapper.Map(entity);
            Assert.That(json.Property("rawJson"), Is.Not.Null);
            Assert.That(json["rawJson"].Type, Is.EqualTo(JTokenType.Object));
            Assert.That(json["rawJson"].ToString(Formatting.None), Is.EqualTo("{\"a\":5}"));

            // map to entity
            entity = new TestClass { RawJson = "5" };
            json = new JObject();
            mapper.Apply(entity, json);
            Assert.That(entity.RawJson, Is.EqualTo("5"));

            entity = new TestClass { RawJson = "5" };
            json = new JObject(new JProperty("rawJson", null));
            mapper.Apply(entity, json);
            Assert.That(entity.RawJson, Is.Null);

            json = new JObject(new JProperty("rawJson", 5));
            entity = mapper.Map(json);
            Assert.That(entity.RawJson, Is.EqualTo("5"));

            json = new JObject(new JProperty("rawJson", true));
            entity = mapper.Map(json);
            Assert.That(entity.RawJson, Is.EqualTo("true"));

            json = new JObject(new JProperty("rawJson", "String"));
            entity = mapper.Map(json);
            Assert.That(entity.RawJson, Is.EqualTo("\"String\""));

            json = new JObject(new JProperty("rawJson", new JArray(1, 2, 3)));
            entity = mapper.Map(json);
            Assert.That(entity.RawJson, Is.EqualTo("[1,2,3]"));

            json = new JObject(new JProperty("rawJson", new JObject(new JProperty("a", 5))));
            entity = mapper.Map(json);
            Assert.That(entity.RawJson, Is.EqualTo("{\"a\":5}"));
        }

        [Test]
        public void ReferenceProperty()
        {
            var mapper = JsonMapperManager.GetMapper<TestClass>();

            // map to JSON
            var entity = new TestClass { InnerReference = null };
            var json = mapper.Map(entity);
            Assert.That(json.Property("innerReference"), Is.Not.Null);
            Assert.That(json["innerReference"].Type, Is.EqualTo(JTokenType.Null));

            entity = new TestClass { InnerReference = new TestInnerClass { Int = 5 } };
            json = mapper.Map(entity);
            Assert.That(json.Property("innerReference"), Is.Not.Null);
            Assert.That(json["innerReference"].Type, Is.EqualTo(JTokenType.Object));
            var jsonInner = (JObject)json["innerReference"];
            Assert.That(jsonInner["int"], Is.Not.Null);
            Assert.That(jsonInner["int"].Type, Is.EqualTo(JTokenType.Integer));
            Assert.That((int)jsonInner["int"], Is.EqualTo(entity.InnerReference.Int));

            // map to entity
            entity = new TestClass { InnerReference = new TestInnerClass { Int = 5 } };
            json = new JObject();
            mapper.Apply(entity, json);
            Assert.That(entity.InnerReference, Is.Not.Null);
            Assert.That(entity.InnerReference.Int, Is.EqualTo(5));

            entity = new TestClass { InnerReference = new TestInnerClass { Int = 5 } };
            json = new JObject(new JProperty("innerReference", null));
            mapper.Apply(entity, json);
            Assert.That(entity.InnerReference, Is.Null);

            json = new JObject(new JProperty("innerReference", new JObject(new JProperty("int", 5))));
            entity = mapper.Map(json);
            Assert.That(entity.InnerReference, Is.Not.Null);
            Assert.That(entity.InnerReference.Int, Is.EqualTo(5));
        }

        [Test]
        public void CollectionProperty()
        {
            var mapper = JsonMapperManager.GetMapper<TestClass>();

            // map to JSON
            var entity = new TestClass { InnerCollection = null };
            var json = mapper.Map(entity);
            Assert.That(json.Property("innerCollection"), Is.Not.Null);
            Assert.That(json["innerCollection"].Type, Is.EqualTo(JTokenType.Null));

            entity = new TestClass { InnerCollection = new List<TestInnerClass> { new TestInnerClass { Int = 5 } } };
            json = mapper.Map(entity);
            Assert.That(json.Property("innerCollection"), Is.Not.Null);
            Assert.That(json["innerCollection"].Type, Is.EqualTo(JTokenType.Array));
            var jsonInner = (JArray)json["innerCollection"];
            Assert.That(jsonInner.Count, Is.EqualTo(1));
            Assert.That(jsonInner[0]["int"], Is.Not.Null);
            Assert.That(jsonInner[0]["int"].Type, Is.EqualTo(JTokenType.Integer));
            Assert.That((int)jsonInner[0]["int"], Is.EqualTo(entity.InnerCollection[0].Int));

            // map to entity
            entity = new TestClass { InnerCollection = new List<TestInnerClass> { new TestInnerClass { Int = 5 } } };
            json = new JObject();
            mapper.Apply(entity, json);
            Assert.That(entity.InnerCollection, Is.Not.Null);
            Assert.That(entity.InnerCollection.Count, Is.EqualTo(1));

            entity = new TestClass { InnerCollection = new List<TestInnerClass> { new TestInnerClass { Int = 5 } } };
            json = new JObject(new JProperty("innerCollection", null));
            mapper.Apply(entity, json);
            Assert.That(entity.InnerCollection, Is.Null);

            json = new JObject(new JProperty("innerCollection", new JArray(new JObject(new JProperty("int", 5)))));
            entity = mapper.Map(json);
            Assert.That(entity.InnerCollection, Is.Not.Null);
            Assert.That(entity.InnerCollection.Count, Is.EqualTo(1));
            Assert.That(entity.InnerCollection[0].Int, Is.EqualTo(5));
        }

        [Test]
        [Explicit]
        public void SpeedTest()
        {
            var mapper = JsonMapperManager.GetMapper<TestClass>();
            var entity = new TestClass
            {
                Int = 5,
                IntNullable = 5,
                Bool = true,
                BoolNullable = true,
                DateTime = DateTime.UtcNow,
                DateTimeNullable = DateTime.UtcNow,
                Guid = Guid.NewGuid(),
                GuidNullable = Guid.NewGuid(),
                Enum = TestEnum.Value1,
                EnumNullable = TestEnum.Value1,
                String = "String",
            };
            var json = new JObject(
                new JProperty("int", 5),
                new JProperty("intNullable", 5),
                new JProperty("bool", true),
                new JProperty("boolNullable", true),
                new JProperty("datetime", "2012-05-03T06:22:34.000000"),
                new JProperty("datetimeNullable", "2012-05-03T06:22:34.000000"),
                new JProperty("guid", Guid.NewGuid().ToString()),
                new JProperty("guidNullable", Guid.NewGuid().ToString()),
                new JProperty("enum", 1),
                new JProperty("enumNullable", 1),
                new JProperty("string", "String"));

            for (var iteration = 0; iteration < 10; iteration++)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                for (var i = 0; i < 1000; i++)
                {
                    mapper.Map(entity);
                }
                stopwatch.Stop();
                var time1 = stopwatch.ElapsedMilliseconds;

                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                for (var i = 0; i < 1000; i++)
                {
                    mapper.Map(json);
                }
                stopwatch.Stop();
                var time2 = stopwatch.ElapsedMilliseconds;

                Console.WriteLine(string.Format("{0} {1}", time1, time2));
            }
        }


        private enum TestEnum
        {
            Value1 = 1,
            Value2 = 2,
        }

        private class TestClass
        {
            public int Int { get; set; }
            public int? IntNullable { get; set; }
            public int? IntNullable2 { get; set; }
            public bool Bool { get; set; }
            public bool? BoolNullable { get; set; }
            public bool? BoolNullable2 { get; set; }
            public Guid Guid { get; set; }
            public Guid? GuidNullable { get; set; }
            public Guid? GuidNullable2 { get; set; }
            public DateTime DateTime { get; set; }
            public DateTime? DateTimeNullable { get; set; }
            public DateTime? DateTimeNullable2 { get; set; }
            public TestEnum Enum { get; set; }
            public TestEnum? EnumNullable { get; set; }
            public TestEnum? EnumNullable2 { get; set; }
            public string String { get; set; }
            public string String2 { get; set; }
            public int[] IntArray { get; set; }
            public string[] StringArray { get; set; }
            public string RawJson { get; set; }
            public TestInnerClass InnerReference { get; set; }
            public List<TestInnerClass> InnerCollection { get; set; }
        }

        private class TestInnerClass
        {
            public int Int { get; set; }
        }
    }
}
