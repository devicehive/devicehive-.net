using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Data.Model;

namespace DeviceHive.API.Mapping
{
    public class JsonMapperConfig
    {
        public static void ConfigureMapping(JsonMapperManager manager)
        {
            manager.Configure<User>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.OneWay)
                .Property(e => e.Login, "login")
                .Property(e => e.Role, "role")
                .Property(e => e.Status, "status")
                .Property(e => e.LastLogin, "lastLogin", JsonMapperEntryMode.OneWay);

            manager.Configure<Network>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.OneWay)
                .Property(e => e.Key, "key") // is returned to administrators only
                .Property(e => e.Name, "name")
                .Property(e => e.Description, "description");

            manager.Configure<UserNetwork>()
                .ReferenceProperty(e => e.Network, "network");

            manager.Configure<DeviceClass>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.OneWay)
                .Property(e => e.Name, "name")
                .Property(e => e.Version, "version")
                .Property(e => e.IsPermanent, "isPermanent")
                .Property(e => e.OfflineTimeout, "offlineTimeout")
                .RawJsonProperty(e => e.Data, "data");

            manager.Configure<Equipment>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.OneWay)
                .Property(e => e.Name, "name")
                .Property(e => e.Code, "code")
                .Property(e => e.Type, "type")
                .RawJsonProperty(e => e.Data, "data");

            manager.Configure<Device>()
                .Property(e => e.GUID, "id", JsonMapperEntryMode.OneWay)
                .Property(e => e.Key, "key", JsonMapperEntryMode.OneWayToSource)
                .Property(e => e.Name, "name")
                .Property(e => e.Status, "status")
                .RawJsonProperty(e => e.Data, "data")
                .ReferenceProperty(e => e.Network, "network")
                .ReferenceProperty(e => e.DeviceClass, "deviceClass");

            manager.Configure<DeviceNotification>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.OneWay)
                .Property(e => e.Timestamp, "timestamp", JsonMapperEntryMode.OneWay)
                .Property(e => e.Notification, "notification")
                .RawJsonProperty(e => e.Parameters, "parameters");

            manager.Configure<DeviceCommand>()
                .Property(e => e.ID, "id", JsonMapperEntryMode.OneWay)
                .Property(e => e.Timestamp, "timestamp", JsonMapperEntryMode.OneWay)
                .Property(e => e.Command, "command")
                .RawJsonProperty(e => e.Parameters, "parameters")
                .Property(e => e.Lifetime, "lifetime")
                .Property(e => e.Flags, "flags")
                .Property(e => e.Status, "status")
                .Property(e => e.Result, "result");

            manager.Configure<DeviceEquipment>()
                .Property(e => e.Code, "id", JsonMapperEntryMode.OneWay)
                .Property(e => e.Timestamp, "timestamp", JsonMapperEntryMode.OneWay)
                .RawJsonProperty(e => e.Parameters, "parameters", JsonMapperEntryMode.OneWay);
        }
    }
}