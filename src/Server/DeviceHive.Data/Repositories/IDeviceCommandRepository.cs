using System;
using System.Collections.Generic;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface IDeviceCommandRepository : ISimpleRepository<DeviceCommand>
    {
        List<DeviceCommand> GetByDevice(int deviceId, DeviceCommandFilter filter = null);
    }
}
