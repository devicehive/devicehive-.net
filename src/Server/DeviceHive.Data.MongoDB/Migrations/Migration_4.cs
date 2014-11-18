using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System.Linq;

namespace DeviceHive.Data.MongoDB.Migrations
{
    [Migration("4")]
    public class Migration_4 : MigrationBase
    {
        public override void Up()
        {
            // create indexes
            Connection.Database.GetCollection("oauth_clients").EnsureIndex(IndexKeys.Ascending("OAuthID"), IndexOptions.SetUnique(true));
            Connection.Database.GetCollection("oauth_grants").EnsureIndex(IndexKeys.Ascending("ClientID"));
            Connection.Database.GetCollection("oauth_grants").EnsureIndex(IndexKeys.Ascending("UserID"));
            Connection.Database.GetCollection("oauth_grants").EnsureIndex(IndexKeys.Ascending("AccessKeyID"));
            Connection.Database.GetCollection("oauth_grants").EnsureIndex(IndexKeys.Ascending("AuthCode"), IndexOptions.SetUnique(true));
        }

        public override void Down()
        {
        }
    }
}
