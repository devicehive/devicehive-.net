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
        #endregion
    }
}
