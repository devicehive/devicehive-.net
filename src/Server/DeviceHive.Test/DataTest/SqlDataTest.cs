using System;
using DeviceHive.Data;
using DeviceHive.Data.EF;
using NUnit.Framework;

namespace DeviceHive.Test.DataTest
{
    [TestFixture]
    public class SqlDataTest : BaseDataTest
    {
        public SqlDataTest()
        {
            DataContext = new DataContext(typeof(TimestampRepository).Assembly);
        }
    }
}
