using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface INetworkRepository : ISimpleRepository<Network>
    {
        List<Network> GetAll(NetworkFilter filter = null);
        List<Network> GetByUser(int userId, NetworkFilter filter = null);
        Network Get(string name);
    }

    public static class NetworkRepositoryExtension
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
