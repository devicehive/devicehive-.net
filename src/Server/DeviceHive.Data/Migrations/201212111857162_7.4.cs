namespace DeviceHive.Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class _74 : DbMigration
    {
        public override void Up()
        {
            CreateIndex("DeviceNotification", "Timestamp");
        }
        
        public override void Down()
        {
            DropIndex("DeviceNotification", "Timestamp");
        }
    }
}
