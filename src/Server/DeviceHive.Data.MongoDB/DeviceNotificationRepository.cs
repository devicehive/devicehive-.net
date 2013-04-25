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
    public class DeviceNotificationRepository : IDeviceNotificationRepository
    {
        private MongoConnection _mongo;

        #region Constructor

        public DeviceNotificationRepository(MongoConnection mongo)
        {
            _mongo = mongo;
        }
        #endregion

        #region IDeviceNotificationRepository Members

        public List<DeviceNotification> GetByDevice(int deviceId, DeviceNotificationFilter filter = null)
        {
            return _mongo.DeviceNotifications.AsQueryable().Where(e => e.DeviceID == deviceId).Filter(filter).ToList();
        }

        public List<DeviceNotification> GetByDevices(int[] deviceIds, DeviceNotificationFilter filter = null)
        {
            var query = _mongo.DeviceNotifications.AsQueryable();
            if (deviceIds != null)
                query = query.Where(e => deviceIds.Contains(e.DeviceID));
            var notifications = query.Filter(filter).ToList();

            var actualDeviceIds = notifications.Select(e => e.DeviceID).Distinct().ToArray();
            var deviceLookup = _mongo.Devices.Find(Query<Device>.In(e => e.ID, actualDeviceIds)).ToDictionary(e => e.ID);

            foreach (var notification in notifications)
                notification.Device = deviceLookup[notification.DeviceID];

            return notifications;
        }

        public DeviceNotification Get(int id)
        {
            return _mongo.DeviceNotifications.FindOneById(id);
        }

        public void Save(DeviceNotification notification)
        {
            if (notification == null)
                throw new ArgumentNullException("notification");

            if (notification.Device != null)
                notification.DeviceID = notification.Device.ID;

            _mongo.EnsureIdentity(notification);
            _mongo.EnsureTimestamp(notification);
            _mongo.DeviceNotifications.Save(notification);
        }

        public void Delete(int id)
        {
            _mongo.DeviceNotifications.Remove(Query<DeviceNotification>.EQ(e => e.ID, id));
        }
        #endregion
    }
}
