using DeviceHive.Core.Mapping;
using DeviceHive.Data.Model;
using Ninject.Activation;

namespace DeviceHive.WebSockets.API.Core
{
    public class JsonMapperConfig
    {
        public static void ConfigureMapping(IContext context, JsonMapperManager manager)
        {
            context.Kernel.ConfigureMapping<User>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.ToJson)
                .Property(e => e.Login, "login")
                .Property(e => e.Role, "role")
                .Property(e => e.Status, "status")
                .Property(e => e.LastLogin, "lastLogin", JsonMapperEntryMode.ToJson);

            context.Kernel.ConfigureMapping<Network>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.ToJson)
                .Property(e => e.Name, "name")
                .Property(e => e.Description, "description");

            context.Kernel.ConfigureMapping<UserNetwork>()
                .ReferenceProperty(e => e.Network, "network");

            context.Kernel.ConfigureMapping<Equipment>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.ToJson)
                .Property(e => e.Name, "name")
                .Property(e => e.Code, "code")
                .Property(e => e.Type, "type")
                .RawJsonProperty(e => e.Data, "data");

            context.Kernel.ConfigureMapping<DeviceClass>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.ToJson)
                .Property(e => e.Name, "name")
                .Property(e => e.Version, "version")
                .Property(e => e.IsPermanent, "isPermanent")
                .Property(e => e.OfflineTimeout, "offlineTimeout")
                .RawJsonProperty(e => e.Data, "data")
                .CollectionProperty(e => e.Equipment, "equipment");

            context.Kernel.ConfigureMapping<Device>()
                .Property(e => e.GUID, "id", JsonMapperEntryMode.ToJson)
                .Property(e => e.Key, "key", JsonMapperEntryMode.FromJson)
                .Property(e => e.Name, "name")
                .Property(e => e.Status, "status")
                .RawJsonProperty(e => e.Data, "data")
                .ReferenceProperty(e => e.Network, "network")
                .ReferenceProperty(e => e.DeviceClass, "deviceClass");

            context.Kernel.ConfigureMapping<DeviceNotification>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.ToJson)
                .Property(e => e.Timestamp, "timestamp", JsonMapperEntryMode.ToJson)
                .Property(e => e.Notification, "notification")
                .RawJsonProperty(e => e.Parameters, "parameters");

            context.Kernel.ConfigureMapping<DeviceCommand>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.ToJson)
                .Property(e => e.Timestamp, "timestamp", JsonMapperEntryMode.ToJson)
                .Property(e => e.UserID, "userId", JsonMapperEntryMode.ToJson)
                .Property(e => e.Command, "command")
                .RawJsonProperty(e => e.Parameters, "parameters")
                .Property(e => e.Lifetime, "lifetime")
                .Property(e => e.Flags, "flags")
                .Property(e => e.Status, "status")
                .RawJsonProperty(e => e.Result, "result");

            context.Kernel.ConfigureMapping<DeviceEquipment>()
                .Property(e => e.Code, "id", JsonMapperEntryMode.ToJson)
                .Property(e => e.Timestamp, "timestamp", JsonMapperEntryMode.ToJson)
                .RawJsonProperty(e => e.Parameters, "parameters", JsonMapperEntryMode.ToJson);

            context.Kernel.ConfigureMapping<ApiInfo>()
                .Property(e => e.ApiVersion, "apiVersion")
                .Property(e => e.ServerTimestamp, "serverTimestamp")
                .Property(e => e.RestServerUrl, "restServerUrl");
        }
    }
}