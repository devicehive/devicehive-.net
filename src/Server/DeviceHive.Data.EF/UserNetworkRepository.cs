using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;

namespace DeviceHive.Data.EF
{
    public class UserNetworkRepository : IUserNetworkRepository
    {
        #region IUserNetworkRepository Members

        public List<UserNetwork> GetByUser(int userId)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.UserNetworks
                    .Include(un => un.User)
                    .Include(un => un.Network)
                    .Where(un => un.UserID == userId)
                    .ToList();
            }
        }

        public List<UserNetwork> GetByNetwork(int networkId)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.UserNetworks
                    .Include(un => un.User)
                    .Include(un => un.Network)
                    .Where(un => un.NetworkID == networkId)
                    .ToList();
            }
        }

        public UserNetwork Get(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.UserNetworks
                    .Include(un => un.User)
                    .Include(un => un.Network)
                    .FirstOrDefault(un => un.ID == id);
            }
        }

        public UserNetwork Get(int userId, int networkId)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.UserNetworks
                    .Include(un => un.User)
                    .Include(un => un.Network)
                    .FirstOrDefault(un => un.UserID == userId && un.NetworkID == networkId);
            }
        }

        public void Save(UserNetwork userNetwork)
        {
            if (userNetwork == null)
                throw new ArgumentNullException("userNetwork");

            using (var context = new DeviceHiveContext())
            {
                context.Users.Attach(userNetwork.User);
                context.Networks.Attach(userNetwork.Network);
                context.UserNetworks.Add(userNetwork);
                if (userNetwork.ID > 0)
                {
                    context.Entry(userNetwork).State = EntityState.Modified;
                }
                context.SaveChanges();
            }
        }

        public void Delete(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                var userNetwork = context.UserNetworks.Find(id);
                if (userNetwork != null)
                {
                    context.UserNetworks.Remove(userNetwork);
                    context.SaveChanges();
                }
            }
        }
        #endregion
    }
}
