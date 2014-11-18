using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;

namespace DeviceHive.Data.MongoDB
{
    public class UserRepository : IUserRepository
    {
        private MongoConnection _mongo;

        #region Constructor

        public UserRepository(MongoConnection mongo)
        {
            _mongo = mongo;
        }
        #endregion

        #region IUserRepository Members

        public List<User> GetAll(UserFilter filter = null)
        {
            return _mongo.Users.AsQueryable().Filter(filter).ToList();
        }

        public User Get(int id)
        {
            return _mongo.Users.FindOneById(id);
        }

        public User Get(string login)
        {
            if (string.IsNullOrEmpty(login))
                throw new ArgumentNullException("login");

            return _mongo.Users.FindOne(Query<User>.EQ(e => e.Login, login));
        }

        public void Save(User user)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            _mongo.EnsureIdentity(user);
            _mongo.Users.Save(user);
        }

        public void Delete(int id)
        {
            _mongo.Users.Remove(Query<User>.EQ(e => e.ID, id));
            _mongo.UserNetworks.Remove(Query<UserNetwork>.EQ(e => e.UserID, id));
            _mongo.AccessKeys.Remove(Query<AccessKey>.EQ(e => e.UserID, id));
        }
        #endregion
    }
}
