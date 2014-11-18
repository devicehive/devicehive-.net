using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
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
            if (filter != null && filter.GridInterval != null)
            {
                // use native MongoDB query for aggregation
                return QueryWithGridInterval(new[] { deviceId }, filter);
            }

            return _mongo.DeviceNotifications.AsQueryable().Where(e => e.DeviceID == deviceId).Filter(filter).ToList();
        }

        public List<DeviceNotification> GetByDevices(int[] deviceIds, DeviceNotificationFilter filter = null)
        {
            List<DeviceNotification> notifications = null;

            if (filter != null && filter.GridInterval != null)
            {
                // use native MongoDB query for aggregation
                notifications = QueryWithGridInterval(deviceIds, filter);
            }
            else
            {
                var query = _mongo.DeviceNotifications.AsQueryable();
                if (deviceIds != null)
                    query = query.Where(e => deviceIds.Contains(e.DeviceID));
                notifications = query.Filter(filter).ToList();
            }

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

        #region Private Methods

        private List<DeviceNotification> QueryWithGridInterval(int[] deviceIds, DeviceNotificationFilter filter)
        {
            if (filter == null)
                throw new ArgumentNullException("filter");
            if (filter.GridInterval == null)
                throw new ArgumentException("GridInterval property of the filter is null!", "filter.GridInterval");

            var periodStart = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);
            var periodMilliseconds = periodStart.Ticks / 10000;

            // prepare a list of operations for aggregation query
            var operations = new List<BsonDocument>();

            // match by devices
            if (deviceIds != null)
            {
                operations.Add(new BsonDocument { { "$match", new BsonDocument { { "DeviceID",
                    new BsonDocument { { "$in", new BsonArray(deviceIds) } } } } } });
            }

            // match by filter criteria
            if (filter.Start != null)
            {
                operations.Add(new BsonDocument { { "$match", new BsonDocument { { "Timestamp",
                    new BsonDocument { { filter.IsDateInclusive ? "$gte" : "$gt", filter.Start.Value } } } } } });
            }
            if (filter.End != null)
            {
                operations.Add(new BsonDocument { { "$match", new BsonDocument { { "Timestamp",
                    new BsonDocument { { filter.IsDateInclusive ? "$lte" : "$lt", filter.End.Value } } } } } });
            }
            if (filter.Notification != null)
            {
                operations.Add(new BsonDocument { { "$match", new BsonDocument { { "Notification", filter.Notification } } } });
            }
            if (filter.Notifications != null)
            {
                operations.Add(new BsonDocument { { "$match", new BsonDocument { { "Notification",
                    new BsonDocument { { "$in", new BsonArray(filter.Notifications) } } } } } });
            }

            // process grid interval aggregation
            operations.Add(new BsonDocument { { "$sort", new BsonDocument { { "Timestamp", 1 } } } });
            operations.Add(new BsonDocument { { "$project", new BsonDocument {
                { "DeviceID", 1 }, { "Timestamp", 1 }, { "Notification", 1 }, { "Parameters", 1 },
                { "tsmod", new BsonDocument { { "$mod", new BsonArray {
                    new BsonDocument { { "$add", new BsonArray {
                        new BsonDocument { { "$subtract", new BsonArray { "$Timestamp", periodStart } } }, periodMilliseconds } } },
                    new BsonInt64(1000 * filter.GridInterval.Value) } } }
                } } } });
            operations.Add(new BsonDocument { { "$project", new BsonDocument {
                { "DeviceID", 1 }, { "Timestamp", 1 }, { "Notification", 1 }, { "Parameters", 1 },
                { "tsinterval", new BsonDocument { { "$subtract", new BsonArray {
                    new BsonDocument { { "$add", new BsonArray {
                        new BsonDocument { { "$subtract", new BsonArray { "$Timestamp", periodStart } } }, periodMilliseconds } } },
                    new BsonString("$tsmod") } } }
                } } } });
            operations.Add(new BsonDocument { { "$group", new BsonDocument {
                { "_id", new BsonDocument { { "tsinterval", "$tsinterval" }, { "DeviceID", "$DeviceID" }, { "Notification", "$Notification" } } },
                { "ID", new BsonDocument { { "$first", "$_id" } } },
                { "Timestamp", new BsonDocument { { "$first", "$Timestamp" } } },
                { "DeviceID", new BsonDocument { { "$first", "$DeviceID" } } },
                { "Notification", new BsonDocument { { "$first", "$Notification" } } },
                { "Parameters", new BsonDocument { { "$first", "$Parameters" } } } } } });
            operations.Add(new BsonDocument { { "$project", new BsonDocument {
                { "_id", "$ID" }, { "DeviceID", 1 }, { "Timestamp", 1 }, { "Notification", 1 }, { "Parameters", 1 } } } });

            // apply sorting and pagination
            if (filter.SortField != DeviceNotificationSortField.None)
            {
                var sortOrder = filter.SortOrder == SortOrder.ASC ? 1 : -1;
                switch (filter.SortField)
                {
                    case DeviceNotificationSortField.Timestamp:
                        operations.Add(new BsonDocument { { "$sort", new BsonDocument { { "Timestamp", sortOrder } } } });
                        break;
                    case DeviceNotificationSortField.Notification:
                        operations.Add(new BsonDocument { { "$sort", new BsonDocument { { "Notification", sortOrder }, { "Timestamp", sortOrder } } } });
                        break;
                }
            }
            if (filter.Skip != null)
            {
                operations.Add(new BsonDocument { { "$skip", filter.Skip.Value } });
            }
            if (filter.Take != null)
            {
                operations.Add(new BsonDocument { { "$limit", filter.Take.Value } });
            }

            // run the aggregation query
            var result = _mongo.DeviceNotifications.Aggregate(operations);
            return result.ResultDocuments.Select(BsonSerializer.Deserialize<DeviceNotification>).ToList();
        }
        #endregion
    }
}
