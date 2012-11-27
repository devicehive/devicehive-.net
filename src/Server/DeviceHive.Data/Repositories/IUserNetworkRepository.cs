using System;
using System.Collections.Generic;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface IUserNetworkRepository : ISimpleRepository<UserNetwork>
    {
        UserNetwork Get(int userId, int networkId);
        List<UserNetwork> GetByUser(int userId);
        List<UserNetwork> GetByNetwork(int networkId);
    }
}
