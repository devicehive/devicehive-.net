using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;

namespace DeviceHive.Data.EF
{
    public class DeviceClassRepository : IDeviceClassRepository
    {
        #region IDeviceClassRepository Members

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
        #endregion
    }
}
