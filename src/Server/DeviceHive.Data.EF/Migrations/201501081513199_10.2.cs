namespace DeviceHive.Data.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _102 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AccessKey", "Type", c => c.Int());
            Sql("update [AccessKey] set [Type] = 0");
            AlterColumn("dbo.AccessKey", "Type", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AccessKey", "Type");
        }
    }
}
