using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface IDeviceClassRepository : ISimpleRepository<DeviceClass>
    {
        List<DeviceClass> GetAll(DeviceClassFilter filter = null);
        DeviceClass Get(string name, string version);
    }

    public static class DeviceClassRepositoryExtension
    {
        public static IQueryable<DeviceClass> Filter(this IQueryable<DeviceClass> query, DeviceClassFilter filter)
        {
            if (filter == null)
                return query;

            if (filter.Name != null)
                query = query.Where(e => e.Name == filter.Name);

            if (filter.NamePattern != null)
                query = query.Where(e => e.Name.Contains(filter.NamePattern));

            if (filter.Version != null)
                query = query.Where(e => e.Version == filter.Version);

            if (filter.SortField != DeviceClassSortField.None)
            {
                switch (filter.SortField)
                {
                    case DeviceClassSortField.ID:
                        query = query.OrderBy(e => e.ID, filter.SortOrder);
                        break;
                    case DeviceClassSortField.Name:
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
