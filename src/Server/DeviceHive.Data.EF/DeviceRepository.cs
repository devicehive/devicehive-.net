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
    public class DeviceRepository : IDeviceRepository
    {
        #region IDeviceRepository Members

        public List<Device> GetAll(DeviceFilter filter = null)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Devices
                    .Include(e => e.Network)
                    .Include(e => e.DeviceClass.Equipment)
                    .Filter(filter).ToList();
            }
        }

        public List<Device> GetByNetwork(int networkId, DeviceFilter filter = null)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Devices
                    .Include(e => e.Network)
                    .Include(e => e.DeviceClass.Equipment)
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
                    .Include(e => e.DeviceClass.Equipment)
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
                    .Include(e => e.DeviceClass.Equipment)
                    .FirstOrDefault(e => e.ID == id);
            }
        }

        public Device Get(string guid)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Devices
                    .Include(e => e.Network)
                    .Include(e => e.DeviceClass.Equipment)
                    .FirstOrDefault(e => e.GUID == guid);
            }
        }

        public void Save(Device device)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (string.IsNullOrEmpty(device.GUID))
                throw new ArgumentException("Device.GUID must have a valid value!", "device.GUID");

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

        public void SetLastOnline(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                context.Database.ExecuteSqlCommand(
                    "update [Device] set [LastOnline] = sysutcdatetime() where [ID] = @ID",
                    new SqlParameter("ID", id));
            }
        }

        public List<Device> GetOfflineDevices()
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Devices
                    .Include(e => e.Network)
                    .Include(e => e.DeviceClass.Equipment)
                    .Where(e => e.DeviceClass.OfflineTimeout != null)
                    .Where(d => d.LastOnline == null || DbFunctions.AddSeconds(d.LastOnline, d.DeviceClass.OfflineTimeout) < DateTime.UtcNow)
                    .ToList();
            }
        }
        #endregion
    }
}
