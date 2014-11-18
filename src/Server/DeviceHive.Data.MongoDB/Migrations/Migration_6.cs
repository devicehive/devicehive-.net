using MongoDB.Bson;
using System;

namespace DeviceHive.Data.MongoDB.Migrations
{
    [Migration("6")]
    public class Migration_6 : MigrationBase
    {
        public override void Up()
        {
            var devices = Connection.Database.GetCollection("devices");
            foreach (var device in devices.FindAll())
            {
                var guid = device["GUID"];
                if (guid.BsonType == BsonType.Binary)
                {
                    device["GUID"] = guid.AsGuid.ToString();
                    devices.Save(device);
                }
            }
        }

        public override void Down()
        {
            var devices = Connection.Database.GetCollection("devices");
            foreach (var device in devices.FindAll())
            {
                var guid = device["GUID"];
                if (guid.BsonType == BsonType.String)
                {
                    Guid parsedGuid;
                    if (Guid.TryParse(guid.AsString, out parsedGuid))
                    {
                        device["GUID"] = parsedGuid;
                        devices.Save(device);
                    }
                    else
                    {
                        Console.WriteLine("Could not change devices.GUID field type to GUID: " + guid.AsString);
                    }
                }
            }
        }
    }
}
