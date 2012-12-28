namespace DeviceHive.Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class _75 : DbMigration
    {
        public override void Up()
        {
            AddColumn("DeviceCommand", "UserID", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("DeviceCommand", "UserID");
        }
    }
}
