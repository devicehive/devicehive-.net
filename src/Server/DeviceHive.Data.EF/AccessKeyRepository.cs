using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;

namespace DeviceHive.Data.EF
{
    public class AccessKeyRepository : IAccessKeyRepository
    {
        #region IAccessKeyRepository Members

        public List<AccessKey> GetByUser(int userId, AccessKeyFilter filter = null)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.AccessKeys
                    .Include(e => e.Permissions)
                    .Where(e => e.UserID == userId).Filter(filter).ToList();
            }
        }

        public AccessKey Get(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.AccessKeys
                    .Include(e => e.Permissions)
                    .FirstOrDefault(e => e.ID == id);
            }
        }

        public AccessKey Get(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException("key");

            using (var context = new DeviceHiveContext())
            {
                return context.AccessKeys
                    .Include(e => e.Permissions)
                    .FirstOrDefault(e => e.Key == key);
            }
        }

        public void Save(AccessKey accessKey)
        {
            if (accessKey == null)
                throw new ArgumentNullException("accessKey");

            using (var context = new DeviceHiveContext())
            {
                context.AccessKeys.Add(accessKey);
                if (accessKey.ID > 0)
                {
                    context.Entry(accessKey).State = EntityState.Modified;
                    
                    foreach (var permission in accessKey.Permissions.Where(e => e.ID > 0))
                    {
                        context.Entry(permission).State = EntityState.Modified;
                    }
                    foreach (var permission in context.AccessKeyPermissions.Where(e => e.AccessKeyID == accessKey.ID))
                    {
                        if (context.Entry(permission).State == EntityState.Unchanged)
                            context.AccessKeyPermissions.Remove(permission);
                    }
                }
                
                context.SaveChanges();
            }
        }

        public void Delete(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                var accessKey = context.AccessKeys.Find(id);
                if (accessKey != null)
                {
                    context.AccessKeys.Remove(accessKey);
                    context.SaveChanges();
                }
            }
        }

        public void Cleanup(DateTime timestamp)
        {
            using (var context = new DeviceHiveContext())
            {
                context.Database.ExecuteSqlCommand("delete from [AccessKey] where [Type] = @Type and [ExpirationDate] < @Timestamp",
                    new SqlParameter("Type", (int)AccessKeyType.Session), new SqlParameter("Timestamp", timestamp));
            }
        }
        #endregion
    }
}
