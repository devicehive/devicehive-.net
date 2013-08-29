using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Linq;

namespace DeviceHive.Data.MongoDB.Migrations
{
    [Migration("2")]
    public class Migration_2 : MigrationBase
    {
        public override void Up()
        {
            // move Equipment objects into DeviceClass objects
            foreach (var deviceClass in Connection.Database.GetCollection("device_classes").FindAll())
            {
                var equipment = Connection.Database.GetCollection("equipment");
                deviceClass["Equipment"] = new BsonArray(equipment.Find(Query.EQ("DeviceClassID", deviceClass["_id"])).ToList());
                Connection.DeviceClasses.Save(deviceClass);

                var devices = Connection.Database.GetCollection("devices");
                devices.Update(Query.EQ("DeviceClassID", deviceClass["_id"]),
                    Update.Set("DeviceClass", deviceClass), new MongoUpdateOptions { Flags = UpdateFlags.Multi });
            }

            // remove Equipment collection
            Connection.Database.DropCollection("equipment");
        }

        public override void Down()
        {
        }
    }
}
