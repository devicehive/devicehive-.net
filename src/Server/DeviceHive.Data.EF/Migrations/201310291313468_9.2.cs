namespace DeviceHive.Data.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _92 : DbMigration
    {
        public override void Up()
        {
            // update Notification.IX_DeviceID_Timestamp index to include the Notification column
            // update Command.IX_DeviceID_Timestamp index to include the Command column
            
            DropIndex("DeviceNotification", new[] { "DeviceID", "Timestamp" });
            DropIndex("DeviceCommand", new[] { "DeviceID", "Timestamp" });

            Sql("CREATE NONCLUSTERED INDEX [IX_DeviceID_Timestamp] ON [DeviceNotification] ( [DeviceID], [Timestamp] ) INCLUDE ([Notification])");
            Sql("CREATE NONCLUSTERED INDEX [IX_DeviceID_Timestamp] ON [DeviceCommand] ( [DeviceID], [Timestamp] ) INCLUDE ([Command])");

            // create index on Command.Timestamp
            CreateIndex("DeviceCommand", new[] { "Timestamp" });
        }
        
        public override void Down()
        {
            DropIndex("DeviceCommand", new[] { "Timestamp" });

            DropIndex("DeviceNotification", new[] { "DeviceID", "Timestamp" });
            DropIndex("DeviceCommand", new[] { "DeviceID", "Timestamp" });

            CreateIndex("DeviceNotification", new[] { "DeviceID", "Timestamp" });
            CreateIndex("DeviceCommand", new[] { "DeviceID", "Timestamp" });
        }
    }
}
