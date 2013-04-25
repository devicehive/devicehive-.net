namespace DeviceHive.Data.EF.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class _71 : DbMigration
    {
        public override void Up()
        {
            AddColumn("DeviceClass", "Data", c => c.String());
            AddColumn("Equipment", "Data", c => c.String());
            AddColumn("Device", "Data", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("Device", "Data");
            DropColumn("Equipment", "Data");
            DropColumn("DeviceClass", "Data");
        }
    }
}
