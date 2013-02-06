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
        public List<DeviceNotification> GetByDevice(int deviceId, DeviceNotificationFilter filter = null)
        {
            using (var context = new DeviceHiveContext())
            {
                var query = context.DeviceNotifications.Where(e => e.Device.ID == deviceId);
                return query.Filter(filter).ToList();
            }
        }

        public List<DeviceNotification> GetByDevices(int[] deviceIds, DeviceNotificationFilter filter = null)
        {
            using (var context = new DeviceHiveContext())
            {
                var query = context.DeviceNotifications.Include(e => e.Device);
                if (deviceIds != null)
                    query = query.Where(e => deviceIds.Contains(e.Device.ID));
                return query.Filter(filter).ToList();
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

    internal static class DeviceNotificationRepositoryExtension
    {
        public static IQueryable<DeviceNotification> Filter(this IQueryable<DeviceNotification> query, DeviceNotificationFilter filter)
        {
            if (filter == null)
                return query;

            if (filter.Start != null)
                query = query.Where(e => e.Timestamp >= filter.Start.Value);

            if (filter.End != null)
                query = query.Where(e => e.Timestamp <= filter.End.Value);

            if (filter.Notification != null)
                query = query.Where(e => e.Notification == filter.Notification);

            if (filter.SortField != DeviceNotificationSortField.None)
            {
                switch (filter.SortField)
                {
                    case DeviceNotificationSortField.Timestamp:
                        query = query.OrderBy(e => e.Timestamp, filter.SortOrder);
                        break;
                    case DeviceNotificationSortField.Notification:
                        query = query.OrderBy(e => e.Notification, filter.SortOrder)
                            .ThenBy(e => e.Timestamp, filter.SortOrder);
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
