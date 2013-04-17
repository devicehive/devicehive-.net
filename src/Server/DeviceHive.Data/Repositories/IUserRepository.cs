using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.Repositories
{
    public interface IUserRepository : ISimpleRepository<User>
    {
        List<User> GetAll(UserFilter filter = null);
        User Get(string login);
    }

    public static class UserRepositoryExtension
    {
        public static IQueryable<User> Filter(this IQueryable<User> query, UserFilter filter)
        {
            if (filter == null)
                return query;

            if (filter.Login != null)
                query = query.Where(e => e.Login == filter.Login);

            if (filter.LoginPattern != null)
                query = query.Where(e => e.Login.Contains(filter.LoginPattern));

            if (filter.Role != null)
                query = query.Where(e => e.Role == filter.Role);

            if (filter.Status != null)
                query = query.Where(e => e.Status == filter.Status);

            if (filter.SortField != UserSortField.None)
            {
                switch (filter.SortField)
                {
                    case UserSortField.ID:
                        query = query.OrderBy(e => e.ID, filter.SortOrder);
                        break;
                    case UserSortField.Login:
                        query = query.OrderBy(e => e.Login, filter.SortOrder);
                        break;
                }
            }

            if (filter.Skip != null)
                query = query.Skip(filter.Skip.Value);

            if (filter.Take != null)
                query = query.Take(filter.Take.Value);

            return query;
        }
    }
}
