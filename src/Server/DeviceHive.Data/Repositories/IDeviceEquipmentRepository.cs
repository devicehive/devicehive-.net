using System;
using System.Collections.Generic;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface IDeviceEquipmentRepository : ISimpleRepository<DeviceEquipment>
    {
        List<DeviceEquipment> GetByDevice(int deviceId);
        DeviceEquipment GetByDeviceAndCode(int deviceId, string code);
    }
}
