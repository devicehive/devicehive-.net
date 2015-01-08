using MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;

namespace DeviceHive.Data.MongoDB.Migrations
{
    [Migration("7")]
    public class Migration_7 : MigrationBase
    {
        public override void Up()
        {
            var deviceCommands = Connection.Database.GetCollection("device_commands");
            deviceCommands.Update(Query.Null, Update.Unset("Flags"), new MongoUpdateOptions { Flags = UpdateFlags.Multi });
        }

        public override void Down()
        {
        }
    }
}
