using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Core.Mapping;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;
using Ninject;
using Ninject.Activation;

namespace DeviceHive.API
{
    public class JsonMapperConfig
    {
        public static void ConfigureMapping(IContext context, JsonMapperManager manager)
        {
            context.Kernel.ConfigureMapping<User>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.OneWay)
                .Property(e => e.Login, "login")
                .Property(e => e.Role, "role")
                .Property(e => e.Status, "status")
                .Property(e => e.LastLogin, "lastLogin", JsonMapperEntryMode.OneWay);

            context.Kernel.ConfigureMapping<Network, NetworkJsonMapper>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.OneWay)
                .Property(e => e.Key, "key") // is returned to administrators only
                .Property(e => e.Name, "name")
                .Property(e => e.Description, "description");

            context.Kernel.ConfigureMapping<UserNetwork>()
                .ReferenceProperty(e => e.Network, "network");

            context.Kernel.ConfigureMapping<DeviceClass>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.OneWay)
                .Property(e => e.Name, "name")
                .Property(e => e.Version, "version")
                .Property(e => e.IsPermanent, "isPermanent")
                .Property(e => e.OfflineTimeout, "offlineTimeout")
                .RawJsonProperty(e => e.Data, "data");

            context.Kernel.ConfigureMapping<Equipment>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.OneWay)
                .Property(e => e.Name, "name")
                .Property(e => e.Code, "code")
                .Property(e => e.Type, "type")
                .RawJsonProperty(e => e.Data, "data");

            context.Kernel.ConfigureMapping<Device>()
                .Property(e => e.GUID, "id", JsonMapperEntryMode.OneWay)
                .Property(e => e.Key, "key", JsonMapperEntryMode.OneWayToSource)
                .Property(e => e.Name, "name")
                .Property(e => e.Status, "status")
                .RawJsonProperty(e => e.Data, "data")
                .ReferenceProperty(e => e.Network, "network")
                .ReferenceProperty(e => e.DeviceClass, "deviceClass");

            context.Kernel.ConfigureMapping<DeviceNotification>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.OneWay)
                .Property(e => e.Timestamp, "timestamp", JsonMapperEntryMode.OneWay)
                .Property(e => e.Notification, "notification")
                .RawJsonProperty(e => e.Parameters, "parameters");

            context.Kernel.ConfigureMapping<DeviceCommand>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.OneWay)
                .Property(e => e.Timestamp, "timestamp", JsonMapperEntryMode.OneWay)
                .Property(e => e.Command, "command")
                .RawJsonProperty(e => e.Parameters, "parameters")
                .Property(e => e.Lifetime, "lifetime")
                .Property(e => e.Flags, "flags")
                .Property(e => e.Status, "status")
                .RawJsonProperty(e => e.Result, "result");

            context.Kernel.ConfigureMapping<DeviceEquipment>()
                .Property(e => e.Code, "id", JsonMapperEntryMode.OneWay)
                .Property(e => e.Timestamp, "timestamp", JsonMapperEntryMode.OneWay)
                .RawJsonProperty(e => e.Parameters, "parameters", JsonMapperEntryMode.OneWay);

            context.Kernel.ConfigureMapping<ApiInfo>()
                .Property(e => e.ApiVersion, "apiVersion")
                .Property(e => e.ServerTimestamp, "serverTimestamp")
                .Property(e => e.WebSocketServerUrl, "webSocketServerUrl");
        }
    }

    /// <summary>
    /// Represents custom mapper for Network entities
    /// </summary>
    public class NetworkJsonMapper : JsonMapper<Network>
    {
        private readonly IKernel _kernel;

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="kernel">Ninject kernel</param>
        /// <param name="configuration">Mapper configuration object</param>
        public NetworkJsonMapper(IKernel kernel, JsonMapperConfiguration<Network> configuration)
            : base(configuration)
        {
            if (kernel == null)
                throw new ArgumentNullException("kernel");

            _kernel = kernel;
        }
        #endregion

        #region JsonMapper<Network> Members

        /// <summary>
        /// Executed after entity is mapped to json object.
        /// Removes the network key if current user is not an administrator
        /// </summary>
        /// <param name="entity">Source entity object</param>
        /// <param name="json">Mapped json object</param>
        protected override void OnAfterMapToJson(Network entity, JObject json)
        {
            base.OnAfterMapToJson(entity, json);

            var context = _kernel.Get<RequestContext>();
            if (context.CurrentUser == null || context.CurrentUser.Role != (int)UserRole.Administrator)
            {
                json.Remove("key"); // do not expose network key to clients
            }
        }
        #endregion
    }
}