using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;

namespace DeviceHive.Data.MongoDB
{
    public class DeviceCommandRepository : IDeviceCommandRepository
    {
        private MongoConnection _mongo;

        #region Constructor

        public DeviceCommandRepository(MongoConnection mongo)
        {
            _mongo = mongo;
        }
        #endregion

        #region Public Methods

        public List<DeviceCommand> GetByDevice(int deviceId, DeviceCommandFilter filter = null)
        {
            return _mongo.DeviceCommands.AsQueryable().Where(e => e.DeviceID == deviceId).Filter(filter).ToList();
        }

        public List<DeviceCommand> GetByDevices(int[] deviceIds, DeviceCommandFilter filter = null)
        {
            var query = _mongo.DeviceCommands.AsQueryable();
            if (deviceIds != null)
                query = query.Where(e => deviceIds.Contains(e.DeviceID));
            var commands = query.Filter(filter).ToList();

            var actualDeviceIds = commands.Select(e => e.DeviceID).Distinct().ToArray();
            var deviceLookup = _mongo.Devices.Find(Query<Device>.In(e => e.ID, actualDeviceIds)).ToDictionary(e => e.ID);

            foreach (var command in commands)
                command.Device = deviceLookup[command.DeviceID];

            return commands;
        }

        public DeviceCommand Get(int id)
        {
            return _mongo.DeviceCommands.FindOneById(id);
        }

        public void Save(DeviceCommand command)
        {
            if (command == null)
                throw new ArgumentNullException("command");

            if (command.Device != null)
                command.DeviceID = command.Device.ID;

            _mongo.EnsureIdentity(command);
            _mongo.EnsureTimestamp(command);
            _mongo.DeviceCommands.Save(command);
        }

        public void Delete(int id)
        {
            _mongo.DeviceCommands.Remove(Query<DeviceCommand>.EQ(e => e.ID, id));
        }

        public void Cleanup(DateTime timestamp)
        {
            _mongo.DeviceCommands.Remove(Query<DeviceCommand>.LT(e => e.Timestamp, timestamp));
        }
        #endregion
    }
}
