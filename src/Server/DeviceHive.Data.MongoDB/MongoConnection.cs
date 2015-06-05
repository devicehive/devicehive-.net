using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using DeviceHive.Data.Model;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace DeviceHive.Data.MongoDB
{
    public class MongoConnection
    {
        private static bool _isClassMapRegistered = false;
        private static object _syncRoot = new object();

        private static string _identityProperty = "ID";
        private static string _timestampProperty = "Timestamp";
        private static ConcurrentDictionary<Type, Func<object, int>> _identityGetters = new ConcurrentDictionary<Type, Func<object, int>>();
        private static ConcurrentDictionary<Type, Action<object, int>> _identitySetters = new ConcurrentDictionary<Type, Action<object, int>>();
        private static ConcurrentDictionary<Type, Func<object, DateTime>> _timestampGetters = new ConcurrentDictionary<Type, Func<object, DateTime>>();
        private static ConcurrentDictionary<Type, Action<object, DateTime>> _timestampSetters = new ConcurrentDictionary<Type, Action<object, DateTime>>();
        private static Dictionary<string, IdentityBlockInfo> _identityBlocks = new Dictionary<string, IdentityBlockInfo>();

        #region Public Properties

        public MongoServer Server { get; private set; }

        public MongoDatabase Database { get; private set; }

        public MongoCollection<User> Users
        {
            get { return Database.GetCollection<User>("users"); }
        }

        public MongoCollection<UserNetwork> UserNetworks
        {
            get { return Database.GetCollection<UserNetwork>("user_networks"); }
        }

        public MongoCollection<AccessKey> AccessKeys
        {
            get { return Database.GetCollection<AccessKey>("access_keys"); }
        }

        public MongoCollection<Network> Networks
        {
            get { return Database.GetCollection<Network>("networks"); }
        }

        public MongoCollection<DeviceClass> DeviceClasses
        {
            get { return Database.GetCollection<DeviceClass>("device_classes"); }
        }

        public MongoCollection<Device> Devices
        {
            get { return Database.GetCollection<Device>("devices"); }
        }

        public MongoCollection<DeviceEquipment> DeviceEquipment
        {
            get { return Database.GetCollection<DeviceEquipment>("device_equipment"); }
        }

        public MongoCollection<DeviceNotification> DeviceNotifications
        {
            get { return Database.GetCollection<DeviceNotification>("device_notifications"); }
        }

        public MongoCollection<DeviceCommand> DeviceCommands
        {
            get { return Database.GetCollection<DeviceCommand>("device_commands"); }
        }

        public MongoCollection<OAuthClient> OAuthClients
        {
            get { return Database.GetCollection<OAuthClient>("oauth_clients"); }
        }

        public MongoCollection<OAuthGrant> OAuthGrants
        {
            get { return Database.GetCollection<OAuthGrant>("oauth_grants"); }
        }
        #endregion

        #region Constructor

        public MongoConnection()
            : this(ConfigurationManager.AppSettings["MongoConnection"])
        {
        }

        public MongoConnection(string connectionString)
            : this(connectionString, connectionString == null ? null : MongoUrl.Create(connectionString).DatabaseName)
        {
        }

        public MongoConnection(string connectionString, string databaseName)
        {
            if (connectionString == null)
                throw new ArgumentNullException("connectionString");
            if (databaseName == null)
                throw new ArgumentNullException("databaseName");

            Server = new MongoClient(connectionString).GetServer();
            Database = Server.GetDatabase(databaseName);

            RegisterClassMap(); // executed once
        }
        #endregion

        #region Public Methods

        public int GetIdentity(string collection, int increment = 1)
        {
            if (string.IsNullOrEmpty(collection))
                throw new ArgumentException("Collection is null or empty!", "collection");

            var counters = Database.GetCollection("counters");
            var fmr = counters.FindAndModify(new FindAndModifyArgs { Query = Query.EQ("_id", collection), Update = Update.Inc("seq", increment) });
            if (fmr.ModifiedDocument == null)
            {
                counters.Insert(new { _id = collection, seq = 1 + increment });
                return 1;
            }

            return (int)fmr.ModifiedDocument["seq"];
        }

        public int GetIdentityFromBlock(string collection, int blockSize = 100)
        {
            if (string.IsNullOrEmpty(collection))
                throw new ArgumentException("Collection is null or empty!", "collection");

            lock (_identityBlocks)
            {
                IdentityBlockInfo block;
                if (!_identityBlocks.TryGetValue(collection, out block) || block.Current == block.Maximum)
                {
                    var current = GetIdentity(collection, increment: blockSize);
                    block = new IdentityBlockInfo { Current = current, Maximum = current + blockSize };
                    _identityBlocks[collection] = block;
                }

                return block.Current++;
            }
        }

        public void SetIdentity<T>(T entity, int identity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            GetIdentitySetter(typeof(T))(entity, identity);
        }

        public void EnsureIdentity<T>(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            var getter = GetIdentityGetter(typeof(T));
            if (getter(entity) == default(int))
            {
                SetIdentity(entity, GetIdentity(typeof(T).Name));
            }
        }

        public DateTime GetCurrentTimestamp()
        {
            var result = Database.GetCollection("timestamp").FindAndModify(new FindAndModifyArgs
            {
                Query = Query.Null,
                Update = Update.CurrentDate("timestamp"),
                VersionReturned = FindAndModifyDocumentVersion.Modified,
                Upsert = true,
            });
            return result.ModifiedDocument["timestamp"].ToUniversalTime();
        }
        
        public void SetTimestamp<T>(T entity, DateTime timestamp)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            GetTimestampSetter(typeof(T))(entity, timestamp);
        }

        public void EnsureTimestamp<T>(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            var getter = GetTimestampGetter(typeof(T));
            if (getter(entity) == default(DateTime))
            {
                SetTimestamp(entity, GetCurrentTimestamp());
            }
        }
        #endregion

        #region Private Methods

        private static void RegisterClassMap()
        {
            if (_isClassMapRegistered)
                return;

            lock (_syncRoot)
            {
                if (_isClassMapRegistered)
                    return;

                var rawJsonSerializer = new RawJsonBsonSerializer();
                BsonClassMap.RegisterClassMap<User>(cm =>
                    {
                        cm.AutoMap();
                        cm.SetIdMember(cm.GetMemberMap(e => e.ID));
                        cm.GetMemberMap(e => e.Data).SetSerializer(rawJsonSerializer);
                    });

                BsonClassMap.RegisterClassMap<UserNetwork>(cm =>
                    {
                        cm.AutoMap();
                        cm.UnmapField(e => e.User);
                        cm.UnmapField(e => e.Network);
                        cm.SetIdMember(cm.GetMemberMap(e => e.ID));
                    });

                BsonClassMap.RegisterClassMap<AccessKey>(cm =>
                    {
                        cm.AutoMap();
                        cm.SetIdMember(cm.GetMemberMap(e => e.ID));
                    });

                BsonClassMap.RegisterClassMap<AccessKeyPermission>(cm =>
                    {
                        cm.AutoMap();
                        cm.SetIdMember(cm.GetMemberMap(e => e.ID));
                    });

                BsonClassMap.RegisterClassMap<Network>(cm =>
                    {
                        cm.AutoMap();
                        cm.SetIdMember(cm.GetMemberMap(e => e.ID));
                    });

                BsonClassMap.RegisterClassMap<DeviceClass>(cm =>
                    {
                        cm.AutoMap();
                        cm.SetIdMember(cm.GetMemberMap(e => e.ID));
                        cm.GetMemberMap(e => e.Data).SetSerializer(rawJsonSerializer);
                    });

                BsonClassMap.RegisterClassMap<Equipment>(cm =>
                    {
                        cm.AutoMap();
                        cm.SetIdMember(cm.GetMemberMap(e => e.ID));
                        cm.GetMemberMap(e => e.Data).SetSerializer(rawJsonSerializer);
                    });

                BsonClassMap.RegisterClassMap<Device>(cm =>
                    {
                        cm.AutoMap();
                        cm.SetIdMember(cm.GetMemberMap(e => e.ID));
                        cm.GetMemberMap(e => e.Data).SetSerializer(rawJsonSerializer);
                    });

                BsonClassMap.RegisterClassMap<DeviceEquipment>(cm =>
                    {
                        cm.AutoMap();
                        cm.UnmapField(e => e.Device);
                        cm.SetIdMember(cm.GetMemberMap(e => e.ID));
                        cm.GetMemberMap(e => e.Parameters).SetSerializer(rawJsonSerializer);
                    });

                BsonClassMap.RegisterClassMap<DeviceNotification>(cm =>
                    {
                        cm.AutoMap();
                        cm.UnmapField(e => e.Device);
                        cm.SetIdMember(cm.GetMemberMap(e => e.ID));
                        cm.GetMemberMap(e => e.Parameters).SetSerializer(rawJsonSerializer);
                    });

                BsonClassMap.RegisterClassMap<DeviceCommand>(cm =>
                    {
                        cm.AutoMap();
                        cm.UnmapField(e => e.Device);
                        cm.SetIdMember(cm.GetMemberMap(e => e.ID));
                        cm.GetMemberMap(e => e.Parameters).SetSerializer(rawJsonSerializer);
                    });

                BsonClassMap.RegisterClassMap<OAuthClient>(cm =>
                    {
                        cm.AutoMap();
                        cm.SetIdMember(cm.GetMemberMap(e => e.ID));
                    });

                BsonClassMap.RegisterClassMap<OAuthGrant>(cm =>
                    {
                        cm.AutoMap();
                        cm.SetIdMember(cm.GetMemberMap(e => e.ID));
                    });

                _isClassMapRegistered = true;
            }
        }

        private static Func<object, int> GetIdentityGetter(Type type)
        {
            Func<object, int> value;
            if (!_identityGetters.TryGetValue(type, out value))
            {
                var entity = Expression.Parameter(typeof(object));
                var method = type.GetProperty(_identityProperty).GetGetMethod();
                var expression = Expression.Call(Expression.Convert(entity, type), method);
                value = Expression.Lambda<Func<object, int>>(expression, entity).Compile();
                _identityGetters[type] = value;
            }
            return value;
        }

        private static Action<object, int> GetIdentitySetter(Type type)
        {
            Action<object, int> value;
            if (!_identitySetters.TryGetValue(type, out value))
            {
                var entity = Expression.Parameter(typeof(object));
                var identity = Expression.Parameter(typeof(int));
                var method = type.GetProperty(_identityProperty).GetSetMethod(true);
                var expression = Expression.Call(Expression.Convert(entity, type), method, identity);
                value = Expression.Lambda<Action<object, int>>(expression, entity, identity).Compile();
                _identitySetters[type] = value;
            }
            return value;
        }

        private static Func<object, DateTime> GetTimestampGetter(Type type)
        {
            Func<object, DateTime> value;
            if (!_timestampGetters.TryGetValue(type, out value))
            {
                var entity = Expression.Parameter(typeof(object));
                var method = type.GetProperty(_timestampProperty).GetGetMethod();
                var expression = Expression.Call(Expression.Convert(entity, type), method);
                value = Expression.Lambda<Func<object, DateTime>>(expression, entity).Compile();
                _timestampGetters[type] = value;
            }
            return value;
        }

        private static Action<object, DateTime> GetTimestampSetter(Type type)
        {
            Action<object, DateTime> value;
            if (!_timestampSetters.TryGetValue(type, out value))
            {
                var entity = Expression.Parameter(typeof(object));
                var timestamp = Expression.Parameter(typeof(DateTime));
                var method = type.GetProperty(_timestampProperty).GetSetMethod(true);
                var expression = Expression.Call(Expression.Convert(entity, type), method, timestamp);
                value = Expression.Lambda<Action<object, DateTime>>(expression, entity, timestamp).Compile();
                _timestampSetters[type] = value;
            }
            return value;
        }
        #endregion

        #region IdentityBlockInfo class
        
        private class IdentityBlockInfo
        {
            public int Current;
            public int Maximum;
        }
        #endregion

        #region RawJsonBsonSerializer class

        private class RawJsonBsonSerializer : IBsonSerializer
        {
            #region IBsonSerializer Members

            public object Deserialize(BsonReader bsonReader, Type nominalType, IBsonSerializationOptions options)
            {
                return Deserialize(bsonReader, nominalType, typeof(string), options);
            }

            public object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType, IBsonSerializationOptions options)
            {
                if (bsonReader.CurrentBsonType == BsonType.Null)
                {
                    bsonReader.ReadNull();
                    return null;
                }

                var bsonValue = BsonSerializer.Deserialize<BsonValue>(bsonReader);
                return bsonValue.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.Strict });
            }

            public IBsonSerializationOptions GetDefaultSerializationOptions()
            {
                throw new NotImplementedException();
            }

            public void Serialize(BsonWriter bsonWriter, Type nominalType, object value, IBsonSerializationOptions options)
            {
                if (value == null)
                {
                    bsonWriter.WriteNull();
                }
                else
                {
                    var bsonValue = BsonSerializer.Deserialize<BsonValue>((string)value);
                    BsonSerializer.Serialize(bsonWriter, bsonValue);
                }
            }
            #endregion
        }
        #endregion
    }
}
