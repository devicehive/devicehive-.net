using System;
using System.Collections.Generic;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface IEquipmentRepository : ISimpleRepository<Equipment>
    {
        List<Equipment> GetByDeviceClass(int deviceClassId);
    }
}
