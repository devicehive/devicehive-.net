using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;

namespace DeviceHive.Data.EF
{
    public class DeviceClassRepository : IDeviceClassRepository
    {
        public List<DeviceClass> GetAll(DeviceClassFilter filter = null)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.DeviceClasses.Filter(filter).ToList();
            }
        }

        public DeviceClass Get(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.DeviceClasses.Find(id);
            }
        }

        public DeviceClass Get(string name, string version)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            using (var context = new DeviceHiveContext())
            {
                return context.DeviceClasses.SingleOrDefault(dc => dc.Name == name && dc.Version == version);
            }
        }

        public void Save(DeviceClass deviceClass)
        {
            if (deviceClass == null)
                throw new ArgumentNullException("deviceClass");

            using (var context = new DeviceHiveContext())
            {
                context.DeviceClasses.Add(deviceClass);
                if (deviceClass.ID > 0)
                {
                    context.Entry(deviceClass).State = EntityState.Modified;
                }
                context.SaveChanges();
            }
        }

        public void Delete(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                var deviceClass = context.DeviceClasses.Find(id);
                if (deviceClass != null)
                {
                    context.DeviceClasses.Remove(deviceClass);
                    context.SaveChanges();
                }
            }
        }
    }

    internal static class DeviceClassRepositoryExtension
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
