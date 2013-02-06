using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;

namespace DeviceHive.Data.EF
{
    public class UserRepository : IUserRepository
    {
        public List<User> GetAll(UserFilter filter = null)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Users.Filter(filter).ToList();
            }
        }

        public User Get(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                return context.Users.Find(id);
            }
        }

        public User Get(string login)
        {
            if (string.IsNullOrEmpty(login))
                throw new ArgumentNullException("login");

            using (var context = new DeviceHiveContext())
            {
                return context.Users.SingleOrDefault(u => u.Login == login);
            }
        }

        public void Save(User user)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            using (var context = new DeviceHiveContext())
            {
                context.Users.Add(user);
                if (user.ID > 0)
                {
                    context.Entry(user).State = EntityState.Modified;
                }
                context.SaveChanges();
            }
        }

        public void Delete(int id)
        {
            using (var context = new DeviceHiveContext())
            {
                var user = context.Users.Find(id);
                if (user != null)
                {
                    context.Users.Remove(user);
                    context.SaveChanges();
                }
            }
        }
    }

    internal static class UserRepositoryExtension
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
