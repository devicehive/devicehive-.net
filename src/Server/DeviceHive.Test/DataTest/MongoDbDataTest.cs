using System;
using DeviceHive.Data;
using DeviceHive.Data.MongoDB;
using NUnit.Framework;

namespace DeviceHive.Test.DataTest
{
    [TestFixture]
    public class MongoDbDataTest : BaseDataTest
    {
        public MongoDbDataTest()
        {
            var connection = new MongoConnection();

            DataContext = new DataContext(typeof(MongoConnection).Assembly);
            DataContext.SetRepositoryCreator(type => Activator.CreateInstance(type, connection));
        }
    }
}
