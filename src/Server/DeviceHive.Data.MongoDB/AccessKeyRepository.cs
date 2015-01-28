using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;

namespace DeviceHive.Data.MongoDB
{
    public class AccessKeyRepository : IAccessKeyRepository
    {
        private MongoConnection _mongo;

        #region Constructor

        public AccessKeyRepository(MongoConnection mongo)
        {
            _mongo = mongo;
        }
        #endregion

        #region IAccessKeyRepository Members

        public List<AccessKey> GetByUser(int userId, AccessKeyFilter filter = null)
        {
            return _mongo.AccessKeys.AsQueryable().Where(e => e.UserID == userId).Filter(filter).ToList();
        }

        public List<AccessKey> GetByUsers(int[] userIds, AccessKeyFilter filter = null)
        {
            return _mongo.AccessKeys.AsQueryable().Where(e => userIds.Contains(e.UserID)).Filter(filter).ToList();
        }

        public AccessKey Get(int id)
        {
            return _mongo.AccessKeys.FindOneById(id);
        }

        public AccessKey Get(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException("key");

            return _mongo.AccessKeys.FindOne(Query<AccessKey>.EQ(e => e.Key, key));
        }

        public void Save(AccessKey accessKey)
        {
            if (accessKey == null)
                throw new ArgumentNullException("accessKey");

            _mongo.EnsureIdentity(accessKey);

            if (accessKey.Permissions == null)
                accessKey.Permissions = new List<AccessKeyPermission>();
            accessKey.Permissions.ForEach(e => _mongo.EnsureIdentity(e));

            _mongo.AccessKeys.Save(accessKey);

            _mongo.OAuthGrants.Update(Query<OAuthGrant>.EQ(e => e.AccessKeyID, accessKey.ID),
                Update<OAuthGrant>.Set(e => e.AccessKey, accessKey), new MongoUpdateOptions { Flags = UpdateFlags.Multi });
        }

        public void Delete(int id)
        {
            _mongo.AccessKeys.Remove(Query<AccessKey>.EQ(e => e.ID, id));
        }

        public void Cleanup(DateTime timestamp)
        {
            _mongo.AccessKeys.Remove(Query.And(
                Query<AccessKey>.EQ(e => e.Type, (int)AccessKeyType.Session),
                Query<AccessKey>.LT(e => e.ExpirationDate, timestamp)));
        }
        #endregion
    }
}
