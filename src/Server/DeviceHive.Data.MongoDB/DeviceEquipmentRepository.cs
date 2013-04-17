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
    public class DeviceEquipmentRepository : IDeviceEquipmentRepository
    {
        private MongoConnection _mongo;

        #region Constructor

        public DeviceEquipmentRepository(MongoConnection mongo)
        {
            _mongo = mongo;
        }
        #endregion

        #region IDeviceEquipmentRepository Members

        public List<DeviceEquipment> GetByDevice(int deviceId)
        {
            return _mongo.DeviceEquipment.Find(Query<DeviceEquipment>.EQ(e => e.DeviceID, deviceId)).ToList();
        }

        public DeviceEquipment GetByDeviceAndCode(int deviceId, string code)
        {
            return _mongo.DeviceEquipment.FindOne(Query.And(
                Query<DeviceEquipment>.EQ(e => e.DeviceID, deviceId),
                Query<DeviceEquipment>.EQ(e => e.Code, code)));
        }

        public DeviceEquipment Get(int id)
        {
            return _mongo.DeviceEquipment.FindOneById(id);
        }

        public void Save(DeviceEquipment equipment)
        {
            if (equipment == null)
                throw new ArgumentNullException("equipment");

            if (equipment.Device != null)
                equipment.DeviceID = equipment.Device.ID;

            _mongo.EnsureIdentity(equipment);
            _mongo.DeviceEquipment.Save(equipment);
        }

        public void Delete(int id)
        {
            _mongo.DeviceEquipment.Remove(Query<DeviceEquipment>.EQ(e => e.ID, id));
        }
        #endregion
    }
}
