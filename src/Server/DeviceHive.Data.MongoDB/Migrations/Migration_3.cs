using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Linq;

namespace DeviceHive.Data.MongoDB.Migrations
{
    [Migration("3")]
    public class Migration_3 : MigrationBase
    {
        public override void Up()
        {
            // create indexes
            Connection.Database.GetCollection("access_keys").CreateIndex(IndexKeys.Ascending("Key"), IndexOptions.SetUnique(true));
            Connection.Database.GetCollection("access_keys").CreateIndex(IndexKeys.Ascending("UserID"));
        }

        public override void Down()
        {
        }
    }
}
