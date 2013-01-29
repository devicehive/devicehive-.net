using System;
using System.Collections.Generic;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface IDeviceClassRepository : ISimpleRepository<DeviceClass>
    {
        List<DeviceClass> GetAll(DeviceClassFilter filter = null);
        DeviceClass Get(string name, string version);
    }
}
