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
            Connection.Database.GetCollection("users").CreateIndex(IndexKeys.Ascending("Login"), IndexOptions.SetUnique(true));
            Connection.Database.GetCollection("user_networks").CreateIndex(IndexKeys.Ascending("NetworkID"));
            Connection.Database.GetCollection("user_networks").CreateIndex(IndexKeys.Ascending("UserID", "NetworkID"), IndexOptions.SetUnique(true));
            Connection.Database.GetCollection("networks").CreateIndex(IndexKeys.Ascending("Name"), IndexOptions.SetUnique(true));
            Connection.Database.GetCollection("device_classes").CreateIndex(IndexKeys.Ascending("Name", "Version"), IndexOptions.SetUnique(true));
            Connection.Database.GetCollection("equipment").CreateIndex(IndexKeys.Ascending("DeviceClassID"));
            Connection.Database.GetCollection("devices").CreateIndex(IndexKeys.Ascending("GUID"), IndexOptions.SetUnique(true));
            Connection.Database.GetCollection("devices").CreateIndex(IndexKeys.Ascending("NetworkID"));
            Connection.Database.GetCollection("devices").CreateIndex(IndexKeys.Ascending("Network.Name"));
            Connection.Database.GetCollection("devices").CreateIndex(IndexKeys.Ascending("DeviceClassID"));
            Connection.Database.GetCollection("devices").CreateIndex(IndexKeys.Ascending("DeviceClass.Name", "DeviceClass.Version"));
            Connection.Database.GetCollection("device_notifications").CreateIndex(IndexKeys.Ascending("Timestamp"));
            Connection.Database.GetCollection("device_notifications").CreateIndex(IndexKeys.Ascending("DeviceID", "Timestamp"));
            Connection.Database.GetCollection("device_commands").CreateIndex(IndexKeys.Ascending("DeviceID", "Timestamp"));
            Connection.Database.GetCollection("device_equipment").CreateIndex(IndexKeys.Ascending("DeviceID", "Code"), IndexOptions.SetUnique(true));

            // create default admin user
            var user = new User("dhadmin", (int)UserRole.Administrator, (int)UserStatus.Active);
            user.SetPassword("dhadmin_#911");
            Connection.EnsureIdentity(user);
            Connection.Users.Save(user);
        }

        public override void Down()
        {
        }
    }
}
