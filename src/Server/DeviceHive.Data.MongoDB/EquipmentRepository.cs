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
    public class EquipmentRepository : IEquipmentRepository
    {
        private MongoConnection _mongo;

        #region Constructor

        public EquipmentRepository(MongoConnection mongo)
        {
            _mongo = mongo;
        }
        #endregion

        #region IEquipmentRepository Members

        public List<Equipment> GetByDeviceClass(int deviceClassId)
        {
            var deviceClass = _mongo.DeviceClasses.FindOneById(deviceClassId);
            if (deviceClass == null)
                return new List<Equipment>();

            var equipment = _mongo.Equipment.Find(Query<Equipment>.EQ(e => e.DeviceClassID, deviceClassId)).ToList();
            foreach (var eq in equipment)
            {
                eq.DeviceClass = deviceClass;
            }

            return equipment;
        }

        public Equipment Get(int id)
        {
            var equipment = _mongo.Equipment.FindOneById(id);
            if (equipment == null)
                return null;

            equipment.DeviceClass = _mongo.DeviceClasses.FindOneById(equipment.DeviceClassID);
            return equipment;
        }

        public void Save(Equipment equipment)
        {
            if (equipment == null)
                throw new ArgumentNullException("equipment");

            if (equipment.DeviceClass != null)
                equipment.DeviceClassID = equipment.DeviceClass.ID;

            _mongo.EnsureIdentity(equipment);
            _mongo.Equipment.Save(equipment);
        }

        public void Delete(int id)
        {
            _mongo.Equipment.Remove(Query<Equipment>.EQ(e => e.ID, id));
        }
        #endregion
    }
}
