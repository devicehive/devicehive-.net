using DeviceHive.Data.Model;
using MongoDB.Driver;
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
            Connection.Database.GetCollection("users").EnsureIndex(IndexKeys.Ascending("Login"), IndexOptions.SetUnique(true));
            Connection.Database.GetCollection("user_networks").EnsureIndex(IndexKeys.Ascending("NetworkID"));
            Connection.Database.GetCollection("user_networks").EnsureIndex(IndexKeys.Ascending("UserID", "NetworkID"), IndexOptions.SetUnique(true));
            Connection.Database.GetCollection("networks").EnsureIndex(IndexKeys.Ascending("Name"), IndexOptions.SetUnique(true));
            Connection.Database.GetCollection("device_classes").EnsureIndex(IndexKeys.Ascending("Name", "Version"), IndexOptions.SetUnique(true));
            Connection.Database.GetCollection("equipment").EnsureIndex(IndexKeys.Ascending("DeviceClassID"));
            Connection.Database.GetCollection("devices").EnsureIndex(IndexKeys.Ascending("GUID"), IndexOptions.SetUnique(true));
            Connection.Database.GetCollection("devices").EnsureIndex(IndexKeys.Ascending("NetworkID"));
            Connection.Database.GetCollection("devices").EnsureIndex(IndexKeys.Ascending("Network.Name"));
            Connection.Database.GetCollection("devices").EnsureIndex(IndexKeys.Ascending("DeviceClassID"));
            Connection.Database.GetCollection("devices").EnsureIndex(IndexKeys.Ascending("DeviceClass.Name", "DeviceClass.Version"));
            Connection.Database.GetCollection("device_notifications").EnsureIndex(IndexKeys.Ascending("Timestamp"));
            Connection.Database.GetCollection("device_notifications").EnsureIndex(IndexKeys.Ascending("DeviceID", "Timestamp"));
            Connection.Database.GetCollection("device_commands").EnsureIndex(IndexKeys.Ascending("DeviceID", "Timestamp"));
            Connection.Database.GetCollection("device_equipment").EnsureIndex(IndexKeys.Ascending("DeviceID", "Code"), IndexOptions.SetUnique(true));

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
