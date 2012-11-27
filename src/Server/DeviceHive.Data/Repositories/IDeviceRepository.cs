using System;
using System.Collections.Generic;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface IDeviceRepository : ISimpleRepository<Device>
    {
        List<Device> GetAll();
        List<Device> GetByNetwork(int networkId);
        Device Get(Guid id);
        List<Device> GetOfflineDevices();
    }
}
