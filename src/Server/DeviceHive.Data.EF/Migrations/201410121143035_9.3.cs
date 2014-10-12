namespace DeviceHive.Data.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _93 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Device", "LastOnline", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Device", "LastOnline");
        }
    }
}
