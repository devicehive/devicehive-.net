using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;

namespace DeviceHive.Data.EF
{
    public class EquipmentRepository : IEquipmentRepository
    {
        #region IEquipmentRepository Members

        public List<Equipment> GetByDeviceClass(int deviceClassId)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Equipments
                    .Include(e => e.DeviceClass)
                    .Where(e => e.DeviceClass.ID == deviceClassId).ToList();
            }
        }

        public Equipment Get(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Equipments
                    .Include(e => e.DeviceClass)
                    .FirstOrDefault(e => e.ID == id);
            }
        }

        public void Save(Equipment equipment)
        {
            if (equipment == null)
                throw new ArgumentNullException("equipment");

            using (var context = new DeviceHiveContext())
            {
                context.DeviceClasses.Attach(equipment.DeviceClass);
                context.Equipments.Add(equipment);
                if (equipment.ID > 0)
                {
                    context.Entry(equipment).State = EntityState.Modified;
                }
                context.SaveChanges();
            }
        }

        public void Delete(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                var equipment = context.Equipments.Find(id);
                if (equipment != null)
                {
                    context.Equipments.Remove(equipment);
                    context.SaveChanges();
                }
            }
        }
        #endregion
    }
}
