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
    public class NetworkRepository : INetworkRepository
    {
        private MongoConnection _mongo;

        #region Constructor

        public NetworkRepository(MongoConnection mongo)
        {
            _mongo = mongo;
        }
        #endregion

        #region INetworkRepository Members

        public List<Network> GetAll(NetworkFilter filter = null)
        {
            return _mongo.Networks.AsQueryable().Filter(filter).ToList();
        }

        public List<Network> GetByUser(int userId, NetworkFilter filter = null)
        {
            var userNetworks = _mongo.UserNetworks.Find(Query<UserNetwork>.EQ(e => e.UserID, userId));
            var networkIds = userNetworks.Select(e => e.NetworkID).Distinct().ToArray();
            return _mongo.Networks.AsQueryable().Where(e => networkIds.Contains(e.ID)).Filter(filter).ToList();
        }

        public Network Get(int id)
        {
            return _mongo.Networks.FindOneById(id);
        }

        public Network Get(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            return _mongo.Networks.FindOne(Query<Network>.EQ(e => e.Name, name));
        }

        public void Save(Network network)
        {
            if (network == null)
                throw new ArgumentNullException("network");

            _mongo.EnsureIdentity(network);
            _mongo.Networks.Save(network);

            _mongo.Devices.Update(Query<Device>.EQ(e => e.NetworkID, network.ID),
                Update<Device>.Set(d => d.Network, network), new MongoUpdateOptions { Flags = UpdateFlags.Multi });
        }

        public void Delete(int id)
        {
            if (_mongo.Devices.FindOne(Query<Device>.EQ(e => e.NetworkID, id)) != null)
                throw new InvalidOperationException("Could not delete a network because there are one or several devices associated with it");

            _mongo.Networks.Remove(Query<Network>.EQ(e => e.ID, id));
            _mongo.UserNetworks.Remove(Query<UserNetwork>.EQ(e => e.NetworkID, id));
        }
        #endregion
    }
}
