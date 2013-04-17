namespace DeviceHive.Data.EF.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class _72 : DbMigration
    {
        public override void Up()
        {
            DropIndex("DeviceNotification", new[] { "DeviceID", "Timestamp" });
            DropIndex("DeviceCommand", new[] { "DeviceID", "Timestamp" });

            Sql("ALTER TABLE [DeviceNotification] ADD CONSTRAINT DF_DeviceNotification_Timestamp DEFAULT sysutcdatetime() FOR [Timestamp]");
            Sql("ALTER TABLE [DeviceCommand] ADD CONSTRAINT DF_DeviceCommand_Timestamp DEFAULT sysutcdatetime() FOR [Timestamp]");

            CreateIndex("DeviceNotification", new[] { "DeviceID", "Timestamp" });
            CreateIndex("DeviceCommand", new[] { "DeviceID", "Timestamp" });
        }
        
        public override void Down()
        {
            DropIndex("DeviceNotification", new[] { "DeviceID", "Timestamp" });
            DropIndex("DeviceCommand", new[] { "DeviceID", "Timestamp" });

            Sql("ALTER TABLE [DeviceNotification] DROP CONSTRAINT DF_DeviceNotification_Timestamp");
            Sql("ALTER TABLE [DeviceCommand] DROP CONSTRAINT DF_DeviceCommand_Timestamp");

            CreateIndex("DeviceNotification", new[] { "DeviceID", "Timestamp" });
            CreateIndex("DeviceCommand", new[] { "DeviceID", "Timestamp" });
        }
    }
}
