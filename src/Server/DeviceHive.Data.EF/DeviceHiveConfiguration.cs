using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Linq;

namespace DeviceHive.Data.EF
{
    public class DeviceHiveConfiguration : DbConfiguration
    {
        public DeviceHiveConfiguration()
        {
            // sets default provider services - required for EF6
            // also fixes a reference to EntityFramework.SqlServer.dll so it will be propagated further during a build
            
            SetProviderServices(SqlProviderServices.ProviderInvariantName, SqlProviderServices.Instance);
        }
    }
}
