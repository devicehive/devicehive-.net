using System;
using System.Collections.Generic;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface IDeviceNotificationRepository : ISimpleRepository<DeviceNotification>
    {
        List<DeviceNotification> GetByDevice(int deviceId, DateTime? start, DateTime? end);
        List<DeviceNotification> GetByDevices(int[] deviceIds, DateTime? start, DateTime? end);
    }
}
