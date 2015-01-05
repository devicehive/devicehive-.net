using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;

namespace DeviceHive.Data.EF
{
    public class UserRepository : IUserRepository
    {
        #region IUserRepository Members

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

        public User GetByFacebookLogin(string facebookLogin)
        {
            if (string.IsNullOrEmpty(facebookLogin))
                throw new ArgumentNullException("facebookLogin");

            using (var context = new DeviceHiveContext())
            {
                return context.Users.SingleOrDefault(u => u.FacebookLogin == facebookLogin);
            }
        }

        public User GetByGoogleLogin(string googleLogin)
        {
            if (string.IsNullOrEmpty(googleLogin))
                throw new ArgumentNullException("googleLogin");

            using (var context = new DeviceHiveContext())
            {
                return context.Users.SingleOrDefault(u => u.GoogleLogin == googleLogin);
            }
        }

        public User GetByGithubLogin(string githubLogin)
        {
            if (string.IsNullOrEmpty(githubLogin))
                throw new ArgumentNullException("githubLogin");

            using (var context = new DeviceHiveContext())
            {
                return context.Users.SingleOrDefault(u => u.GithubLogin == githubLogin);
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
        #endregion
    }
}
