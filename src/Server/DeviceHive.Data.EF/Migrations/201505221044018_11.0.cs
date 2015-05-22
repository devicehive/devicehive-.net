namespace DeviceHive.Data.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _110 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Device", "IsBlocked", c => c.Boolean(nullable: true));
            Sql("update dbo.Device set IsBlocked = cast(0 as bit)");
            AlterColumn("dbo.Device", "IsBlocked", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Device", "IsBlocked");
        }
    }
}
