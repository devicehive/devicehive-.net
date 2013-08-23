using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface IDeviceCommandRepository : ISimpleRepository<DeviceCommand>
    {
        List<DeviceCommand> GetByDevice(int deviceId, DeviceCommandFilter filter = null);
        List<DeviceCommand> GetByDevices(int[] deviceIds, DeviceCommandFilter filter = null);
    }

    public static class DeviceCommandRepositoryExtension
    {
        public static IQueryable<DeviceCommand> Filter(this IQueryable<DeviceCommand> query, DeviceCommandFilter filter)
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

            if (filter.Command != null)
                query = query.Where(e => e.Command == filter.Command);

            if (filter.Status != null)
                query = query.Where(e => e.Status == filter.Status);

            if (filter.SortField != DeviceCommandSortField.None)
            {
                switch (filter.SortField)
                {
                    case DeviceCommandSortField.Timestamp:
                        query = query.OrderBy(e => e.Timestamp, filter.SortOrder);
                        break;
                    case DeviceCommandSortField.Command:
                        query = query.OrderBy(e => e.Command, filter.SortOrder)
                            .ThenBy(e => e.Timestamp, filter.SortOrder);
                        break;
                    case DeviceCommandSortField.Status:
                        query = query.OrderBy(e => e.Status, filter.SortOrder)
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
