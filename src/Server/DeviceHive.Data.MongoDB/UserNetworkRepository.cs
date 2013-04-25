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
    public class UserNetworkRepository : IUserNetworkRepository
    {
        private MongoConnection _mongo;

        #region Constructor

        public UserNetworkRepository(MongoConnection mongo)
        {
            _mongo = mongo;
        }
        #endregion

        #region IUserNetworkRepository

        public List<UserNetwork> GetByUser(int userId)
        {
            var user = _mongo.Users.FindOneById(userId);
            if (user == null)
                return new List<UserNetwork>();

            var userNetworks = _mongo.UserNetworks.Find(Query<UserNetwork>.EQ(e => e.UserID, userId)).ToList();
            if (userNetworks.Any())
            {
                var networkIds = userNetworks.Select(n => n.NetworkID).Distinct().ToList();
                var networkLookup = _mongo.Networks.Find(Query<Network>.In(e => e.ID, networkIds)).ToDictionary(e => e.ID);
                foreach (var userNetwork in userNetworks)
                {
                    userNetwork.User = user;
                    userNetwork.Network = networkLookup[userNetwork.NetworkID];
                }
            }

            return userNetworks;
        }

        public List<UserNetwork> GetByNetwork(int networkId)
        {
            var network = _mongo.Networks.FindOneById(networkId);
            if (network == null)
                return new List<UserNetwork>();

            var userNetworks = _mongo.UserNetworks.Find(Query<UserNetwork>.EQ(e => e.NetworkID, networkId)).ToList();
            if (userNetworks.Any())
            {
                var userIds = userNetworks.Select(n => n.UserID).Distinct().ToList();
                var userLookup = _mongo.Users.Find(Query<User>.In(e => e.ID, userIds)).ToDictionary(e => e.ID);
                foreach (var userNetwork in userNetworks)
                {
                    userNetwork.Network = network;
                    userNetwork.User = userLookup[userNetwork.UserID];
                }
            }

            return userNetworks;
        }

        public UserNetwork Get(int id)
        {
            var userNetwork = _mongo.UserNetworks.FindOneById(id);
            if (userNetwork == null)
                return null;

            userNetwork.User = _mongo.Users.FindOneById(userNetwork.UserID);
            userNetwork.Network = _mongo.Networks.FindOneById(userNetwork.NetworkID);
            return userNetwork;
        }

        public UserNetwork Get(int userId, int networkId)
        {
            var userNetwork = _mongo.UserNetworks.FindOne(Query.And(
                Query<UserNetwork>.EQ(e => e.UserID, userId),
                Query<UserNetwork>.EQ(e => e.NetworkID, networkId)));
            if (userNetwork == null)
                return null;

            userNetwork.User = _mongo.Users.FindOneById(userId);
            userNetwork.Network = _mongo.Networks.FindOneById(networkId);
            return userNetwork;
        }

        public void Save(UserNetwork userNetwork)
        {
            if (userNetwork == null)
                throw new ArgumentNullException("userNetwork");

            if (userNetwork.User != null)
                userNetwork.UserID = userNetwork.User.ID;
            if (userNetwork.Network != null)
                userNetwork.NetworkID = userNetwork.Network.ID;

            _mongo.EnsureIdentity(userNetwork);
            _mongo.UserNetworks.Save(userNetwork);
        }

        public void Delete(int id)
        {
            _mongo.UserNetworks.Remove(Query<UserNetwork>.EQ(e => e.ID, id));
        }
        #endregion
    }
}
