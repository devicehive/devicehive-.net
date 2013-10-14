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
    public class OAuthClientRepository : IOAuthClientRepository
    {
        private MongoConnection _mongo;

        #region Constructor

        public OAuthClientRepository(MongoConnection mongo)
        {
            _mongo = mongo;
        }
        #endregion

        #region IOAuthClientRepository Members

        public List<OAuthClient> GetAll(OAuthClientFilter filter = null)
        {
            return _mongo.OAuthClients.AsQueryable().Filter(filter).ToList();
        }

        public OAuthClient Get(int id)
        {
            return _mongo.OAuthClients.FindOneById(id);
        }

        public OAuthClient Get(string oauthId)
        {
            if (string.IsNullOrEmpty(oauthId))
                throw new ArgumentNullException("oauthId");

            return _mongo.OAuthClients.FindOne(Query<OAuthClient>.EQ(e => e.OAuthID, oauthId));
        }

        public void Save(OAuthClient oauthClient)
        {
            if (oauthClient == null)
                throw new ArgumentNullException("oauthClient");

            _mongo.EnsureIdentity(oauthClient);
            _mongo.OAuthClients.Save(oauthClient);

            _mongo.OAuthGrants.Update(Query<OAuthGrant>.EQ(e => e.ClientID, oauthClient.ID),
                Update<OAuthGrant>.Set(e => e.Client, oauthClient), new MongoUpdateOptions { Flags = UpdateFlags.Multi });
        }

        public void Delete(int id)
        {
            _mongo.OAuthClients.Remove(Query<OAuthClient>.EQ(e => e.ID, id));
            _mongo.OAuthGrants.Remove(Query<OAuthGrant>.EQ(e => e.ClientID, id));
        }
        #endregion
    }
}
