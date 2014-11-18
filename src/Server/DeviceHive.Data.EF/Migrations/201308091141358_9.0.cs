namespace DeviceHive.Data.EF.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class _90 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "AccessKey",
                c => new
                {
                    ID = c.Int(nullable: false, identity: true),
                    Label = c.String(nullable: false, maxLength: 64),
                    Key = c.String(nullable: false, maxLength: 48),
                    UserID = c.Int(nullable: false),
                    ExpirationDate = c.DateTime(),
                })
                .PrimaryKey(t => t.ID)
                .ForeignKey("User", t => t.UserID, cascadeDelete: true)
                .Index(t => t.UserID)
                .Index(t => t.Key, unique: true);

            CreateTable(
                "AccessKeyPermission",
                c => new
                {
                    ID = c.Int(nullable: false, identity: true),
                    AccessKeyID = c.Int(nullable: false),
                    Configuration = c.String(nullable: false),
                })
                .PrimaryKey(t => t.ID)
                .ForeignKey("AccessKey", t => t.AccessKeyID, cascadeDelete: true)
                .Index(t => t.AccessKeyID);

        }

        public override void Down()
        {
            DropIndex("AccessKeyPermission", new[] { "AccessKeyID" });
            DropIndex("AccessKey", new[] { "UserID" });
            DropIndex("AccessKey", new[] { "Key" });
            DropForeignKey("AccessKeyPermission", "AccessKeyID", "AccessKey");
            DropForeignKey("AccessKey", "UserID", "User");
            DropTable("AccessKeyPermission");
            DropTable("AccessKey");
        }
    }
}
