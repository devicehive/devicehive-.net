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
    public class DeviceCommandRepository : IDeviceCommandRepository
    {
        #region IDeviceCommandRepository

        public List<DeviceCommand> GetByDevice(int deviceId, DeviceCommandFilter filter = null)
        {
            using (var context = new DeviceHiveContext())
            {
                var query = context.DeviceCommands.Where(e => e.Device.ID == deviceId);
                return query.Filter(filter).ToList();
            }
        }

        public List<DeviceCommand> GetByDevices(int[] deviceIds, DeviceCommandFilter filter = null)
        {
            using (var context = new DeviceHiveContext())
            {
                var query = context.DeviceCommands.Include(e => e.Device);
                if (deviceIds != null)
                    query = query.Where(e => deviceIds.Contains(e.Device.ID));
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

        public void Cleanup(DateTime timestamp)
        {
            using (var context = new DeviceHiveContext())
            {
                context.Database.CommandTimeout = 300;
                context.Database.ExecuteSqlCommand("delete from [DeviceCommand] where [Timestamp] < @Timestamp", new SqlParameter("Timestamp", timestamp));
            }
        }
        #endregion
    }
}
