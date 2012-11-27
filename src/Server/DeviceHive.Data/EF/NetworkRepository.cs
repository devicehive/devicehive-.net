using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;

namespace DeviceHive.Data.EF
{
    public class NetworkRepository : INetworkRepository
    {
        public List<Network> GetAll()
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Networks.ToList();
            }
        }

        public List<Network> GetByUser(int userId)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Networks.Where(n => context.UserNetworks
                    .Where(un => un.UserID == userId).Select(un => un.NetworkID).Contains(n.ID)).ToList();
            }
        }

        public Network Get(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Networks.Find(id);
            }
        }

        public Network Get(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            using (var context = new DeviceHiveContext())
            {
                return context.Networks.SingleOrDefault(u => u.Name == name);
            }
        }

        public Network GetByKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException("key");

            using (var context = new DeviceHiveContext())
            {
                return context.Networks.SingleOrDefault(u => u.Key == key);
            }
        }

        public void Save(Network network)
        {
            if (network == null)
                throw new ArgumentNullException("network");

            using (var context = new DeviceHiveContext())
            {
                context.Networks.Add(network);
                if (network.ID > 0)
                {
                    context.Entry(network).State = EntityState.Modified;
                }
                context.SaveChanges();
            }
        }

        public void Delete(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                var network = context.Networks.Find(id);
                if (network != null)
                {
                    context.Networks.Remove(network);
                    context.SaveChanges();
                }
            }
        }
    }
}
