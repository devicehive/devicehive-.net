using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;

namespace DeviceHive.Data.EF
{
    public class OAuthClientRepository : IOAuthClientRepository
    {
        #region IOAuthClientRepository Members

        public List<OAuthClient> GetAll(OAuthClientFilter filter = null)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.OAuthClients.Filter(filter).ToList();
            }
        }

        public OAuthClient Get(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.OAuthClients.Find(id);
            }
        }

        public OAuthClient Get(string oauthId)
        {
            if (string.IsNullOrEmpty(oauthId))
                throw new ArgumentNullException("oauthId");

            using (var context = new DeviceHiveContext())
            {
                return context.OAuthClients.SingleOrDefault(u => u.OAuthID == oauthId);
            }
        }

        public void Save(OAuthClient oauthClient)
        {
            if (oauthClient == null)
                throw new ArgumentNullException("oauthClient");

            using (var context = new DeviceHiveContext())
            {
                context.OAuthClients.Add(oauthClient);
                if (oauthClient.ID > 0)
                {
                    context.Entry(oauthClient).State = EntityState.Modified;
                }
                context.SaveChanges();
            }
        }

        public void Delete(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                var oauthClient = context.OAuthClients.Find(id);
                if (oauthClient != null)
                {
                    context.OAuthClients.Remove(oauthClient);
                    context.SaveChanges();
                }
            }
        }
        #endregion
    }
}
