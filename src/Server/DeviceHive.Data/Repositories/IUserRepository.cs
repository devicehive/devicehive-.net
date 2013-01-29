using System;
using System.Collections.Generic;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface IUserRepository : ISimpleRepository<User>
    {
        List<User> GetAll(UserFilter filter = null);
        User Get(string login);
    }
}
