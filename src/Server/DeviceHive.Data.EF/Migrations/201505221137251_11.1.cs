namespace DeviceHive.Data.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _111 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.User", "Data", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.User", "Data");
        }
    }
}
