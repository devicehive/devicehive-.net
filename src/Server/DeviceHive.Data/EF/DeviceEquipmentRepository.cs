using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;

namespace DeviceHive.Data.EF
{
    public class DeviceEquipmentRepository : IDeviceEquipmentRepository
    {
        public List<DeviceEquipment> GetByDevice(int deviceId)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.DeviceEquipments.Where(e => e.Device.ID == deviceId).ToList();
            }
        }

        public DeviceEquipment GetByDeviceAndCode(int deviceId, string code)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.DeviceEquipments.FirstOrDefault(e => e.Device.ID == deviceId && e.Code == code);
            }
        }

        public DeviceEquipment Get(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.DeviceEquipments.Find(id);
            }
        }

        public void Save(DeviceEquipment equipment)
        {
            if (equipment == null)
                throw new ArgumentNullException("equipment");

            using (var context = new DeviceHiveContext())
            {
                context.Devices.Attach(equipment.Device);
                context.DeviceEquipments.Add(equipment);
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
                var equipment = context.DeviceEquipments.Find(id);
                if (equipment != null)
                {
                    context.DeviceEquipments.Remove(equipment);
                    context.SaveChanges();
                }
            }
        }
    }
}
