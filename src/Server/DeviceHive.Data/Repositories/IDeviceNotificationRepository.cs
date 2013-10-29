using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface IDeviceNotificationRepository : ISimpleRepository<DeviceNotification>
    {
        List<DeviceNotification> GetByDevice(int deviceId, DeviceNotificationFilter filter = null);
        List<DeviceNotification> GetByDevices(int[] deviceIds, DeviceNotificationFilter filter = null);
    }

    public static class DeviceNotificationRepositoryExtension
    {
        public static IQueryable<DeviceNotification> Filter(this IQueryable<DeviceNotification> query, DeviceNotificationFilter filter,
            Func<IQueryable<DeviceNotification>, IQueryable<DeviceNotification>> additionalFilter = null)
        {
            if (filter == null)
                return query;

            if (filter.Start != null)
            {
                var start = DateTime.SpecifyKind(filter.Start.Value, DateTimeKind.Utc);
                if (!filter.IsDateInclusive)
                    start = start.AddTicks(10); // SQL Server has 7-digit precision, while JSON mapping 6-digit
                query = filter.IsDateInclusive ? query.Where(e => e.Timestamp >= start) : query.Where(e => e.Timestamp > start);
            }

            if (filter.End != null)
            {
                var end = DateTime.SpecifyKind(filter.End.Value, DateTimeKind.Utc);
                if (!filter.IsDateInclusive)
                    end = end.AddTicks(-10); // SQL Server has 7-digit precision, while JSON mapping 6-digit
                query = filter.IsDateInclusive ? query.Where(e => e.Timestamp <= end) : query.Where(e => e.Timestamp < end);
            }

            if (filter.Notification != null)
                query = query.Where(e => e.Notification == filter.Notification);

            if (filter.Notifications != null)
                query = query.Where(e => filter.Notifications.Contains(e.Notification));

            if (additionalFilter != null)
                query = additionalFilter(query);

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
