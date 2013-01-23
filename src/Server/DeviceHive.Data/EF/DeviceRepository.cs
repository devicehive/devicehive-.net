using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Objects;
using System.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;

namespace DeviceHive.Data.EF
{
    public class DeviceRepository : IDeviceRepository
    {
        public List<Device> GetAll(DeviceFilter filter = null)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Devices
                    .Include(e => e.Network)
                    .Include(e => e.DeviceClass)
                    .Filter(filter).ToList();
            }
        }

        public List<Device> GetByNetwork(int networkId, DeviceFilter filter = null)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Devices
                    .Include(e => e.Network)
                    .Include(e => e.DeviceClass)
                    .Where(e => e.Network.ID == networkId)
                    .Filter(filter).ToList();
            }
        }

        public List<Device> GetByUser(int userId, DeviceFilter filter = null)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Devices
                    .Include(e => e.Network)
                    .Include(e => e.DeviceClass)
                    .Where(e => context.UserNetworks.Any(n => n.UserID == userId && n.NetworkID == e.NetworkID))
                    .Filter(filter).ToList();
            }
        }

        public Device Get(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Devices
                    .Include(e => e.Network)
                    .Include(e => e.DeviceClass)
                    .FirstOrDefault(e => e.ID == id);
            }
        }

        public Device Get(Guid guid)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Devices
                    .Include(e => e.Network)
                    .Include(e => e.DeviceClass)
                    .FirstOrDefault(e => e.GUID == guid);
            }
        }

        public void Save(Device device)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (device.GUID == Guid.Empty)
                throw new ArgumentException("Device.ID must have a valid value!", "device.ID");

            using (var context = new DeviceHiveContext())
            {
                if (device.Network != null)
                {
                    context.Networks.Attach(device.Network);
                }
                context.DeviceClasses.Attach(device.DeviceClass);
                context.Devices.Add(device);
                if (device.ID > 0)
                {
                    context.Entry(device).State = EntityState.Modified;
                }
                context.SaveChanges();
            }
        }

        public void Delete(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                var device = context.Devices.Find(id);
                if (device != null)
                {
                    context.Devices.Remove(device);
                    context.SaveChanges();
                }
            }
        }

        public List<Device> GetOfflineDevices()
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Devices
                    .Include(e => e.Network)
                    .Include(e => e.DeviceClass)
                    .Where(e => e.DeviceClass.OfflineTimeout != null)
                    .Where(d => !context.DeviceNotifications.Any(n => n.Device == d &&
                        EntityFunctions.AddSeconds(n.Timestamp, d.DeviceClass.OfflineTimeout) >= DateTime.UtcNow))
                    .ToList();
            }
        }
    }

    internal static class DeviceRepositoryExtension
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
                query = query.Where(e => e.NetworkID == filter.NetworkID);

            if (filter.NetworkName != null)
                query = query.Where(e => e.Network.Name == filter.NetworkName);

            if (filter.DeviceClassID != null)
                query = query.Where(e => e.DeviceClassID == filter.DeviceClassID);

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
