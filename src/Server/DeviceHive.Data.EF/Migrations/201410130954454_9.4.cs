namespace DeviceHive.Data.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _94 : DbMigration
    {
        public override void Up()
        {
            DropIndex("Device", "IX_GUID");
            AlterColumn("dbo.Device", "GUID", c => c.String(nullable: false, maxLength: 64));
            CreateIndex("dbo.Device", "GUID", true);
        }

        public override void Down()
        {
            DropIndex("Device", "IX_GUID");
            AlterColumn("dbo.Device", "GUID", c => c.Guid(nullable: false));
            CreateIndex("dbo.Device", "GUID", true);
        }
    }
}
