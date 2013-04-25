namespace DeviceHive.Data.EF.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class _70 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("Device", "NetworkID", c => c.Int());
            DropIndex("Network", new[] { "Key" });
        }
        
        public override void Down()
        {
            AlterColumn("Device", "NetworkID", c => c.Int(nullable: false));
            CreateIndex("Network", "Key", false);
        }
    }
}
