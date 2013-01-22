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
        public List<Device> GetAll()
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Devices
                    .Include(e => e.Network)
                    .Include(e => e.DeviceClass)
                    .ToList();
            }
        }

        public List<Device> GetByNetwork(int networkId)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Devices
                    .Include(e => e.Network)
                    .Include(e => e.DeviceClass)
                    .Where(e => e.Network.ID == networkId).ToList();
            }
        }

        public List<Device> GetByUser(int userId)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Devices
                    .Include(e => e.Network)
                    .Include(e => e.DeviceClass)
                    .Where(e => context.UserNetworks
                        .Any(n => n.UserID == userId && n.NetworkID == e.NetworkID))
                    .ToList();
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
}
