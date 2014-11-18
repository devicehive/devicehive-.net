using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;

namespace DeviceHive.Data.EF
{
    public class OAuthGrantRepository : IOAuthGrantRepository
    {
        #region IOAuthGrantRepository Members

        public List<OAuthGrant> GetByUser(int userId, OAuthGrantFilter filter = null)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.OAuthGrants
                    .Include(e => e.Client)
                    .Include(e => e.AccessKey.Permissions)
                    .Where(e => e.UserID == userId)
                    .Filter(filter).ToList();
            }
        }

        public OAuthGrant Get(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.OAuthGrants
                    .Include(e => e.Client)
                    .Include(e => e.AccessKey.Permissions)
                    .FirstOrDefault(e => e.ID == id);
            }
        }

        public OAuthGrant Get(Guid authCode)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.OAuthGrants
                    .Include(e => e.Client)
                    .Include(e => e.AccessKey.Permissions)
                    .FirstOrDefault(e => e.AuthCode == authCode);
            }
        }

        public void Save(OAuthGrant oauthGrant)
        {
            if (oauthGrant == null)
                throw new ArgumentNullException("oauthGrant");

            using (var context = new DeviceHiveContext())
            {
                context.OAuthClients.Attach(oauthGrant.Client);
                context.AccessKeys.Attach(oauthGrant.AccessKey);
                context.OAuthGrants.Add(oauthGrant);
                if (oauthGrant.ID > 0)
                {
                    context.Entry(oauthGrant).State = EntityState.Modified;
                }
                context.SaveChanges();
            }
        }

        public void Delete(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                var oauthGrant = context.OAuthGrants.Find(id);
                if (oauthGrant != null)
                {
                    context.OAuthGrants.Remove(oauthGrant);
                    context.SaveChanges();
                }
            }
        }
        #endregion
    }
}
