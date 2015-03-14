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
    public class DeviceRepository : IDeviceRepository
    {
        private MongoConnection _mongo;

        #region Constructor

        public DeviceRepository(MongoConnection mongo)
        {
            _mongo = mongo;
        }
        #endregion

        #region IDeviceRepository Members

        public List<Device> GetAll(DeviceFilter filter = null)
        {
            return _mongo.Devices.AsQueryable().Filter(filter).ToList();
        }

        public List<Device> GetByNetwork(int networkId, DeviceFilter filter = null)
        {
            return _mongo.Devices.AsQueryable().Where(e => e.NetworkID == networkId).Filter(filter).ToList();
        }

        public List<Device> GetByUser(int userId, DeviceFilter filter = null)
        {
            var networkIds = _mongo.UserNetworks.AsQueryable().Where(e => e.UserID == userId).Select(e => (int?)e.NetworkID).ToArray();
            return _mongo.Devices.AsQueryable().Where(e => networkIds.Contains(e.NetworkID)).Filter(filter).ToList();
        }

        public Device Get(int id)
        {
            return _mongo.Devices.FindOneById(id);
        }

        public Device Get(string guid)
        {
            return _mongo.Devices.FindOne(Query<Device>.EQ(e => e.GUID, guid));
        }

        public void Save(Device device)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (string.IsNullOrEmpty(device.GUID))
                throw new ArgumentException("Device.GUID must have a valid value!", "device.GUID");

            if (device.Network != null)
            {
                device.NetworkID = device.Network.ID;
            }
            else if (device.NetworkID != null)
            {
                device.Network = _mongo.Networks.FindOneById(device.NetworkID.Value);
                if (device.Network == null)
                    throw new ArgumentException("Specified NetworkID does not exist!", "device.NetworkID");
            }

            if (device.DeviceClass != null)
            {
                device.DeviceClassID = device.DeviceClass.ID;
            }
            else
            {
                device.DeviceClass = _mongo.DeviceClasses.FindOneById(device.DeviceClassID);
                if (device.DeviceClass == null)
                    throw new ArgumentException("Specified DeviceClassID does not exist!", "device.DeviceClassID");
            }

            _mongo.EnsureIdentity(device);
            _mongo.Devices.Save(device);
        }

        public void Delete(int id)
        {
            _mongo.Devices.Remove(Query<Device>.EQ(e => e.ID, id));
            _mongo.DeviceEquipment.Remove(Query<DeviceEquipment>.EQ(e => e.DeviceID, id));
            _mongo.DeviceNotifications.Remove(Query<DeviceNotification>.EQ(e => e.DeviceID, id));
            _mongo.DeviceCommands.Remove(Query<DeviceCommand>.EQ(e => e.DeviceID, id));
        }

        public void SetLastOnline(int id)
        {
            _mongo.Database.Eval(new EvalArgs { Code = string.Format("db.devices.update({{ _id: {0} }}, {{ $set: {{ LastOnline: new Date() }}}});", id), Lock = false });
        }

        public List<Device> GetOfflineDevices()
        {
            var devices = _mongo.Devices.AsQueryable()
                .Where(d => d.DeviceClass.OfflineTimeout != null)
                .Select(d => new { ID = d.ID, LastOnline = d.LastOnline, OfflineTimeout = d.DeviceClass.OfflineTimeout }).ToList();

            var deviceIds = new List<int>();
            var timestamp = _mongo.Database.Eval(new EvalArgs { Code = "return new Date()", Lock = false }).ToUniversalTime();
            foreach (var device in devices)
            {
                if (device.LastOnline == null || device.LastOnline < timestamp.AddSeconds(-device.OfflineTimeout.Value))
                    deviceIds.Add(device.ID);
            }

            return _mongo.Devices.Find(Query<Device>.In(e => e.ID, deviceIds)).ToList();
        }
        #endregion
    }
}
