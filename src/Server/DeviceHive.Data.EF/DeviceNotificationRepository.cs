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
        #region IDeviceNotificationRepository Members

        public List<DeviceNotification> GetByDevice(int deviceId, DeviceNotificationFilter filter = null)
        {
            using (var context = new DeviceHiveContext())
            {
                var query = context.DeviceNotifications.Where(e => e.Device.ID == deviceId);
                return query.Filter(filter, FilterByGridInterval(filter == null ? null : filter.GridInterval)).ToList();
            }
        }

        public List<DeviceNotification> GetByDevices(int[] deviceIds, DeviceNotificationFilter filter = null)
        {
            using (var context = new DeviceHiveContext())
            {
                var query = context.DeviceNotifications.Include(e => e.Device);
                if (deviceIds != null)
                    query = query.Where(e => deviceIds.Contains(e.Device.ID));
                return query.Filter(filter, FilterByGridInterval(filter == null ? null : filter.GridInterval)).ToList();
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
        #endregion

        #region Private Methods

        private Func<IQueryable<DeviceNotification>, IQueryable<DeviceNotification>> FilterByGridInterval(int? gridInterval)
        {
            if (gridInterval == null)
                return null;

            var periodStart = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);
            var periodSeconds = periodStart.Ticks / 10000000;
            return query => query.OrderBy(n => n.Timestamp).GroupBy(n => new
                {
                    n.DeviceID,
                    n.Notification,
                    Interval = (periodSeconds + DbFunctions.DiffSeconds(n.Timestamp, periodStart)) / gridInterval
                }).Select(g => g.FirstOrDefault());
        }
        #endregion
    }
}
