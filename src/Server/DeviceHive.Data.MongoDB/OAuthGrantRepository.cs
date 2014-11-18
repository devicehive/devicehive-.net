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
    public class OAuthGrantRepository : IOAuthGrantRepository
    {
        private MongoConnection _mongo;

        #region Constructor

        public OAuthGrantRepository(MongoConnection mongo)
        {
            _mongo = mongo;
        }
        #endregion

        #region IOAuthGrantRepository Members

        public List<OAuthGrant> GetByUser(int userId, OAuthGrantFilter filter = null)
        {
            return _mongo.OAuthGrants.AsQueryable().Where(e => e.UserID == userId).Filter(filter).ToList();
        }

        public OAuthGrant Get(int id)
        {
            return _mongo.OAuthGrants.FindOneById(id);
        }

        public OAuthGrant Get(Guid authCode)
        {
            return _mongo.OAuthGrants.FindOne(Query<OAuthGrant>.EQ(e => e.AuthCode, authCode));
        }

        public void Save(OAuthGrant oauthGrant)
        {
            if (oauthGrant == null)
                throw new ArgumentNullException("oauthGrant");

            if (oauthGrant.Client != null)
            {
                oauthGrant.ClientID = oauthGrant.Client.ID;
            }
            else
            {
                oauthGrant.Client = _mongo.OAuthClients.FindOneById(oauthGrant.ClientID);
                if (oauthGrant.Client == null)
                    throw new ArgumentException("Specified ClientID does not exist!", "oauthGrant.ClientID");
            }

            if (oauthGrant.AccessKey != null)
            {
                oauthGrant.AccessKeyID = oauthGrant.AccessKey.ID;
            }
            else
            {
                oauthGrant.AccessKey = _mongo.AccessKeys.FindOneById(oauthGrant.AccessKeyID);
                if (oauthGrant.AccessKey == null)
                    throw new ArgumentException("Specified AccessKeyID does not exist!", "oauthGrant.AccessKeyID");
            }

            _mongo.EnsureIdentity(oauthGrant);
            _mongo.OAuthGrants.Save(oauthGrant);
        }

        public void Delete(int id)
        {
            _mongo.OAuthGrants.Remove(Query<OAuthGrant>.EQ(e => e.ID, id));
        }
        #endregion
    }
}
