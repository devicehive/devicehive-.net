using System;
using System.Collections.Generic;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface IDeviceCommandRepository : ISimpleRepository<DeviceCommand>
    {
        DateTime GetCurrentTimestamp();
        List<DeviceCommand> GetByDevice(int deviceId, DateTime? start, DateTime? end);
    }
}
