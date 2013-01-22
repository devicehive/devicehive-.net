using System;
using System.Collections.Generic;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface IDeviceRepository : ISimpleRepository<Device>
    {
        Device Get(Guid guid);
        List<Device> GetAll();
        List<Device> GetByNetwork(int networkId);
        List<Device> GetByUser(int userId);
        List<Device> GetOfflineDevices();
    }
}
