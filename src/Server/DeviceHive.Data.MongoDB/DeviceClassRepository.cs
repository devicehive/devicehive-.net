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
    public class DeviceClassRepository : IDeviceClassRepository
    {
        private MongoConnection _mongo;

        #region Constructor

        public DeviceClassRepository(MongoConnection mongo)
        {
            _mongo = mongo;
        }
        #endregion

        #region IDeviceClassRepository Members

        public List<DeviceClass> GetAll(DeviceClassFilter filter = null)
        {
            return _mongo.DeviceClasses.AsQueryable().Filter(filter).ToList();
        }

        public DeviceClass Get(int id)
        {
            return _mongo.DeviceClasses.FindOneById(id);
        }

        public DeviceClass Get(string name, string version)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            return _mongo.DeviceClasses.FindOne(Query.And(
                Query<DeviceClass>.EQ(e => e.Name, name),
                Query<DeviceClass>.EQ(e => e.Version, version)));
        }

        public void Save(DeviceClass deviceClass)
        {
            if (deviceClass == null)
                throw new ArgumentNullException("deviceClass");

            _mongo.EnsureIdentity(deviceClass);
            _mongo.DeviceClasses.Save(deviceClass);

            _mongo.Devices.Update(Query<Device>.EQ(e => e.DeviceClassID, deviceClass.ID),
                Update<Device>.Set(d => d.DeviceClass, deviceClass), new MongoUpdateOptions { Flags = UpdateFlags.Multi });
        }

        public void Delete(int id)
        {
            if (_mongo.Devices.FindOne(Query<Device>.EQ(e => e.DeviceClassID, id)) != null)
                throw new InvalidOperationException("Could not delete a device class because there are one or several devices associated with it");

            _mongo.DeviceClasses.Remove(Query<DeviceClass>.EQ(e => e.ID, id));
            _mongo.Equipment.Remove(Query<Equipment>.EQ(e => e.DeviceClassID, id));
        }
        #endregion
    }
}
