using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;

namespace DeviceHive.Data.EF
{
    public class DeviceNotificationRepository : IDeviceNotificationRepository
    {
        public List<DeviceNotification> GetByDevice(int deviceId, DateTime? start, DateTime? end)
        {
            using (var context = new DeviceHiveContext())
            {
                var query = context.DeviceNotifications.Where(e => e.Device.ID == deviceId);
                if (start != null)
                    query = query.Where(e => e.Timestamp >= start.Value);
                if (end != null)
                    query = query.Where(e => e.Timestamp <= end.Value);
                return query.ToList();
            }
        }

        public DeviceNotification Get(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.DeviceNotifications.Find(id);
            }
        }

        public void Save(DeviceNotification notification)
        {
            if (notification == null)
                throw new ArgumentNullException("notification");

            using (var context = new DeviceHiveContext())
            {
                context.Devices.Attach(notification.Device);
                context.DeviceNotifications.Add(notification);
                if (notification.ID > 0)
                {
                    context.Entry(notification).State = EntityState.Modified;
                }
                context.SaveChanges();
            }
        }

        public void Delete(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                var notification = context.DeviceNotifications.Find(id);
                if (notification != null)
                {
                    context.DeviceNotifications.Remove(notification);
                    context.SaveChanges();
                }
            }
        }
    }
}
