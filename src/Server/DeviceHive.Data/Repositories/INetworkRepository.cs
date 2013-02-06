using System;
using System.Collections.Generic;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface INetworkRepository : ISimpleRepository<Network>
    {
        List<Network> GetAll(NetworkFilter filter = null);
        List<Network> GetByUser(int userId, NetworkFilter filter = null);
        Network Get(string name);
    }
}
