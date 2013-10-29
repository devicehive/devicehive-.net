using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Linq;

namespace DeviceHive.Data.MongoDB.Migrations
{
    [Migration("5")]
    public class Migration_5 : MigrationBase
    {
        public override void Up()
        {
            // create indexes
            Connection.Database.GetCollection("device_commands").EnsureIndex(IndexKeys.Ascending("Timestamp"));
        }

        public override void Down()
        {
        }
    }
}
