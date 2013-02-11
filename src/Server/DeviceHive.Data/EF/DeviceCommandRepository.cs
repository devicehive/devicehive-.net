using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;

namespace DeviceHive.Data.EF
{
    public class DeviceCommandRepository : IDeviceCommandRepository
    {
        public List<DeviceCommand> GetByDevice(int deviceId, DeviceCommandFilter filter = null)
        {
            using (var context = new DeviceHiveContext())
            {
                var query = context.DeviceCommands.Where(e => e.Device.ID == deviceId);
                return query.Filter(filter).ToList();
            }
        }

        public DeviceCommand Get(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.DeviceCommands.Find(id);
            }
        }

        public void Save(DeviceCommand command)
        {
            if (command == null)
                throw new ArgumentNullException("command");

            using (var context = new DeviceHiveContext())
            {
                context.Devices.Attach(command.Device);
                context.DeviceCommands.Add(command);
                if (command.ID > 0)
                {
                    context.Entry(command).State = EntityState.Modified;
                }
                context.SaveChanges();
            }
        }

        public void Delete(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                var command = context.DeviceCommands.Find(id);
                if (command != null)
                {
                    context.DeviceCommands.Remove(command);
                    context.SaveChanges();
                }
            }
        }
    }

    internal static class DeviceCommandRepositoryExtension
    {
        public static IQueryable<DeviceCommand> Filter(this IQueryable<DeviceCommand> query, DeviceCommandFilter filter)
        {
            if (filter == null)
                return query;

            if (filter.Start != null)
                query = query.Where(e => e.Timestamp >= filter.Start.Value);

            if (filter.End != null)
                query = query.Where(e => e.Timestamp <= filter.End.Value);

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
