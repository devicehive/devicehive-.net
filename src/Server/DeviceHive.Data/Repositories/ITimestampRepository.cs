using System;

namespace DeviceHive.Data.Repositories
{
    public interface ITimestampRepository
    {
        DateTime GetCurrentTimestamp();
    }
}
