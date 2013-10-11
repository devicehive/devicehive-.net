namespace DeviceHive.Data.EF.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class _91 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "OAuthClient",
                c => new
                {
                    ID = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, maxLength: 128),
                    Domain = c.String(nullable: false, maxLength: 128),
                    Subnet = c.String(maxLength: 128),
                    RedirectUri = c.String(nullable: false, maxLength: 128),
                    OAuthID = c.String(nullable: false, maxLength: 32),
                    OAuthSecret = c.String(nullable: false, maxLength: 32),
                })
                .PrimaryKey(t => t.ID)
                .Index(t => t.OAuthID, unique: true);

            CreateTable(
                "OAuthGrant",
                c => new
                {
                    ID = c.Int(nullable: false, identity: true),
                    Timestamp = c.DateTime(nullable: false, storeType: "datetime2"),
                    AuthCode = c.Guid(),
                    ClientID = c.Int(nullable: false),
                    UserID = c.Int(nullable: false),
                    AccessKeyID = c.Int(nullable: false),
                    Type = c.Int(nullable: false),
                    AccessType = c.Int(nullable: false),
                    RedirectUri = c.String(maxLength: 128),
                    Scope = c.String(nullable: false, maxLength: 256),
                    NetworkList = c.String(maxLength: 128),
                })
                .PrimaryKey(t => t.ID)
                .ForeignKey("OAuthClient", t => t.ClientID, cascadeDelete: true)
                .ForeignKey("User", t => t.UserID, cascadeDelete: true)
                .ForeignKey("AccessKey", t => t.AccessKeyID, cascadeDelete: false)
                .Index(t => t.ClientID)
                .Index(t => t.UserID)
                .Index(t => t.AccessKeyID);

            Sql("CREATE UNIQUE NONCLUSTERED INDEX [IX_AuthCode] ON [OAuthGrant] ( [AuthCode] ASC ) WHERE [AuthCode] IS NOT NULL");
        }

        public override void Down()
        {
            DropIndex("OAuthGrant", new[] { "AccessKeyID" });
            DropIndex("OAuthGrant", new[] { "UserID" });
            DropIndex("OAuthGrant", new[] { "ClientID" });
            DropIndex("OAuthGrant", new[] { "AuthCode" });
            DropForeignKey("OAuthGrant", "AccessKeyID", "AccessKey");
            DropForeignKey("OAuthGrant", "UserID", "User");
            DropForeignKey("OAuthGrant", "ClientID", "OAuthClient");
            DropTable("OAuthGrant");
            DropIndex("OAuthClient", new[] { "OAuthID" });
            DropTable("OAuthClient");
        }
    }
}
