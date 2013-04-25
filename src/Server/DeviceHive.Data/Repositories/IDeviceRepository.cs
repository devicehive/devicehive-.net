using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface IDeviceRepository : ISimpleRepository<Device>
    {
        Device Get(Guid guid);
        List<Device> GetAll(DeviceFilter filter = null);
        List<Device> GetByNetwork(int networkId, DeviceFilter filter = null);
        List<Device> GetByUser(int userId, DeviceFilter filter = null);
        List<Device> GetOfflineDevices();
    }

    public static class DeviceRepositoryExtension
    {
        public static IQueryable<Device> Filter(this IQueryable<Device> query, DeviceFilter filter)
        {
            if (filter == null)
                return query;

            if (filter.Name != null)
                query = query.Where(e => e.Name == filter.Name);

            if (filter.NamePattern != null)
                query = query.Where(e => e.Name.Contains(filter.NamePattern));

            if (filter.Status != null)
                query = query.Where(e => e.Status == filter.Status);

            if (filter.NetworkID != null)
                query = query.Where(e => e.NetworkID == filter.NetworkID.Value);

            if (filter.NetworkName != null)
                query = query.Where(e => e.Network.Name == filter.NetworkName);

            if (filter.DeviceClassID != null)
                query = query.Where(e => e.DeviceClassID == filter.DeviceClassID.Value);

            if (filter.DeviceClassName != null)
                query = query.Where(e => e.DeviceClass.Name == filter.DeviceClassName);

            if (filter.DeviceClassVersion != null)
                query = query.Where(e => e.DeviceClass.Version == filter.DeviceClassVersion);

            if (filter.SortField != DeviceSortField.None)
            {
                switch (filter.SortField)
                {
                    case DeviceSortField.ID:
                        query = query.OrderBy(e => e.ID, filter.SortOrder);
                        break;
                    case DeviceSortField.Name:
                        query = query.OrderBy(e => e.Name, filter.SortOrder);
                        break;
                    case DeviceSortField.Status:
                        query = query.OrderBy(e => e.Status, filter.SortOrder)
                            .ThenBy(e => e.Name, filter.SortOrder);
                        break;
                    case DeviceSortField.Network:
                        query = query.OrderBy(e => e.Network.Name, filter.SortOrder)
                            .ThenBy(e => e.Name, filter.SortOrder);
                        break;
                    case DeviceSortField.DeviceClass:
                        query = query.OrderBy(e => e.DeviceClass.Name, filter.SortOrder)
                            .ThenBy(e => e.DeviceClass.Version, filter.SortOrder)
                            .ThenBy(e => e.Name, filter.SortOrder);
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
