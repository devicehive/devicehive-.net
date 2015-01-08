namespace DeviceHive.Data.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _101 : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.DeviceCommand", "Flags");
        }
        
        public override void Down()
        {
            AddColumn("dbo.DeviceCommand", "Flags", c => c.Int());
        }
    }
}
