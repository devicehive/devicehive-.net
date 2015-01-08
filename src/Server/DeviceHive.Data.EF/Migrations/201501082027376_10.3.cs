namespace DeviceHive.Data.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _103 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Device", "Key", c => c.String(maxLength: 64));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Device", "Key", c => c.String(nullable: false, maxLength: 64));
        }
    }
}
