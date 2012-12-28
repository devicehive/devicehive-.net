using System;
using System.Collections.Generic;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface INetworkRepository : ISimpleRepository<Network>
    {
        List<Network> GetAll();
        List<Network> GetByUser(int userId);
        Network Get(string name);
    }
}
