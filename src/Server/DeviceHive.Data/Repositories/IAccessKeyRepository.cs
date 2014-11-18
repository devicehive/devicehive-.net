using System;
using System.Collections.Generic;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface IAccessKeyRepository : ISimpleRepository<AccessKey>
    {
        List<AccessKey> GetByUser(int userId);
        AccessKey Get(string key);
    }
}
