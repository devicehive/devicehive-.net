using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using MongoDB.Bson;

namespace DeviceHive.Data.MongoDB
{
    public class TimestampRepository : ITimestampRepository
    {
        private MongoConnection _mongo;

        #region Constructor

        public TimestampRepository(MongoConnection mongo)
        {
            _mongo = mongo;
        }
        #endregion

        #region ITimestampRepository Members

        public DateTime GetCurrentTimestamp()
        {
            return _mongo.Database.Eval(EvalFlags.NoLock, "return new Date()").ToUniversalTime();
        }
        #endregion
    }
}
