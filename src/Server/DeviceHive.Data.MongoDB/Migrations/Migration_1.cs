using DeviceHive.Data.Model;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeviceHive.Data.MongoDB.Migrations
{
    [Migration("1")]
    public class Migration_1 : MigrationBase
    {
        public override void Up()
        {
            // create indexes
            Connection.Users.EnsureIndex(IndexKeys<User>.Ascending(e => e.Login), IndexOptions.SetUnique(true));
            Connection.UserNetworks.EnsureIndex(IndexKeys<UserNetwork>.Ascending(e => e.NetworkID));
            Connection.UserNetworks.EnsureIndex(IndexKeys<UserNetwork>.Ascending(e => e.UserID, e => e.NetworkID), IndexOptions.SetUnique(true));
            Connection.Networks.EnsureIndex(IndexKeys<Network>.Ascending(e => e.Name), IndexOptions.SetUnique(true));
            Connection.DeviceClasses.EnsureIndex(IndexKeys<DeviceClass>.Ascending(e => e.Name, e => e.Version), IndexOptions.SetUnique(true));
            Connection.Equipment.EnsureIndex(IndexKeys<Equipment>.Ascending(e => e.DeviceClassID));
            Connection.Devices.EnsureIndex(IndexKeys<Device>.Ascending(e => e.GUID), IndexOptions.SetUnique(true));
            Connection.Devices.EnsureIndex(IndexKeys<Device>.Ascending(e => e.NetworkID));
            Connection.Devices.EnsureIndex(IndexKeys<Device>.Ascending(e => e.Network.Name));
            Connection.Devices.EnsureIndex(IndexKeys<Device>.Ascending(e => e.DeviceClassID));
            Connection.Devices.EnsureIndex(IndexKeys<Device>.Ascending(e => e.DeviceClass.Name, e => e.DeviceClass.Version));
            Connection.DeviceNotifications.EnsureIndex(IndexKeys<DeviceNotification>.Ascending(e => e.Timestamp));
            Connection.DeviceNotifications.EnsureIndex(IndexKeys<DeviceNotification>.Ascending(e => e.DeviceID, e => e.Timestamp));
            Connection.DeviceCommands.EnsureIndex(IndexKeys<DeviceCommand>.Ascending(e => e.DeviceID, e => e.Timestamp));
            Connection.DeviceEquipment.EnsureIndex(IndexKeys<DeviceEquipment>.Ascending(e => e.DeviceID, e => e.Code), IndexOptions.SetUnique(true));

            // create default admin user
            var user = new User("dhadmin", "dhadmin_#911", (int)UserRole.Administrator, (int)UserStatus.Active);
            Connection.EnsureIdentity(user);
            Connection.Users.Save(user);
        }

        public override void Down()
        {
        }
    }
}
