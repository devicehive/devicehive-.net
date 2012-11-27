namespace DeviceHive.Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class _61 : DbMigration
    {
        public override void Up()
        {
            AddColumn("DeviceClass", "OfflineTimeout", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("DeviceClass", "OfflineTimeout");
        }
    }
}
