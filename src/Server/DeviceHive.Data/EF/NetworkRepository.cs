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
        public List<Network> GetAll(NetworkFilter filter = null)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Networks.Filter(filter).ToList();
            }
        }

        public List<Network> GetByUser(int userId, NetworkFilter filter = null)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Networks.Where(n => context.UserNetworks
                    .Where(un => un.UserID == userId).Select(un => un.NetworkID).Contains(n.ID))
                    .Filter(filter).ToList();
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

    internal static class NetworkRepositoryExtension
    {
        public static IQueryable<Network> Filter(this IQueryable<Network> query, NetworkFilter filter)
        {
            if (filter == null)
                return query;

            if (filter.Name != null)
                query = query.Where(e => e.Name == filter.Name);

            if (filter.NamePattern != null)
                query = query.Where(e => e.Name.Contains(filter.NamePattern));

            if (filter.SortField != NetworkSortField.None)
            {
                switch (filter.SortField)
                {
                    case NetworkSortField.ID:
                        query = query.OrderBy(e => e.ID, filter.SortOrder);
                        break;
                    case NetworkSortField.Name:
                        query = query.OrderBy(e => e.Name, filter.SortOrder);
                        break;
                }
            }

            if (filter.Skip != null)
                query = query.Skip(filter.Skip.Value);

            if (filter.Take != null)
                query = query.Take(filter.Take.Value);

            return query;
        }
    }
}
