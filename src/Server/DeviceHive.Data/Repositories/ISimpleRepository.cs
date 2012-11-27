using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeviceHive.Data.Repositories
{
    public interface ISimpleRepository<T>
    {
        T Get(int id);
        void Save(T entity);
        void Delete(int id);
    }
}
