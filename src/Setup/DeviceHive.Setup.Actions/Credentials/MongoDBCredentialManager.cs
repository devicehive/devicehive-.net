using System;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace DeviceHive.Setup.Actions.Credentials
{
    public class MongoDBCredentialManager : CredentialManager
    {
        #region Constructor
        public MongoDBCredentialManager(string connectionString)
            : base(connectionString)
        {
        }

        #endregion

        protected override void UpdateCredentials(string login, string passwordHash, string passwordSalt)
        {
            var mongoDb = new MongoClient(ConnectionString).GetServer();
            var db = mongoDb.GetDatabase(new MongoUrl(ConnectionString).DatabaseName);
            var collection = db.GetCollection<BsonDocument>("users");
            var user = collection.FindOne(Query.EQ("Login", login));
            if (user == null)
            {
                throw new Exception("User not found!");
            }

            user["PasswordHash"] = passwordHash;
            user["PasswordSalt"] = passwordSalt;
            collection.Save(user);
        }
    }
}
