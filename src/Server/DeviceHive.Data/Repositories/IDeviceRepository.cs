using System;
using System.Collections.Generic;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface IDeviceRepository : ISimpleRepository<Device>
    {
        Device Get(Guid guid);
        List<Device> GetAll(DeviceFilter filter = null);
        List<Device> GetByNetwork(int networkId, DeviceFilter filter = null);
        List<Device> GetByUser(int userId, DeviceFilter filter = null);
        List<Device> GetOfflineDevices();
    }
}
