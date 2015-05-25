using System;
using MongoDB.Driver;

namespace DeviceHive.Setup.Actions
{
    public class MongoDatabaseValidator
    {
        private MongoServer _mongoServer;

        public MongoDatabaseValidator(MongoServer mongoServer)
        {
            if (mongoServer == null)
                throw new ArgumentNullException("mongoServer");

            _mongoServer = mongoServer;
        }

        public void Validate(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentNullException("databaseName");

            if (!_mongoServer.DatabaseExists(databaseName))
                throw new Exception(string.Format("Database '{0}' does not exist. Please enter a correct database name.", databaseName));
        }
    }
}
