namespace DeviceHive.Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class _6 : DbMigration
    {
        public override void Up()
        {
            // network
            AddColumn("Network", "Key", c => c.String(maxLength: 64));
            CreateIndex("Network", "Key", false);

            // device class
            AddColumn("DeviceClass", "IsPermanent", c => c.Boolean());
            Sql("update DeviceClass set IsPermanent = 0");
            AlterColumn("DeviceClass", "IsPermanent", c => c.Boolean(nullable: false));
            AlterColumn("DeviceClass", "Version", c => c.String(nullable: false, maxLength: 32));
            DropIndex("DeviceClass", new[] { "Name" });
            CreateIndex("DeviceClass", new[] { "Name", "Version" }, true);
            
            // device
            AddColumn("Device", "Key", c => c.String(maxLength: 64));
            Sql("update [Device] set [Key] = N'key'");
            AlterColumn("Device", "Key", c => c.String(nullable: false, maxLength: 64));

            // equipment
            DropForeignKey("Equipment", "EquipmentTypeID", "EquipmentType");
            DropIndex("Equipment", new[] { "EquipmentTypeID" });
            AddColumn("Equipment", "Type", c => c.String(nullable: false, maxLength: 128));
            DropColumn("Equipment", "EquipmentTypeID");
            DropTable("EquipmentType");

            // user
            CreateTable(
                "User",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Login = c.String(nullable: false, maxLength: 64),
                        PasswordHash = c.String(nullable: false, maxLength: 48),
                        PasswordSalt = c.String(nullable: false, maxLength: 24),
                        Role = c.Int(nullable: false),
                        Status = c.Int(nullable: false),
                        LoginAttempts = c.Int(nullable: false),
                        LastLogin = c.DateTime(storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.ID)
                .Index(t => t.Login, true);

            // user network
            CreateTable(
                "UserNetwork",
                c => new
                {
                    ID = c.Int(nullable: false, identity: true),
                    UserID = c.Int(nullable: false),
                    NetworkID = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.ID)
                .ForeignKey("User", t => t.UserID, cascadeDelete: true)
                .ForeignKey("Network", t => t.NetworkID, cascadeDelete: true)
                .Index(t => new { t.UserID, t.NetworkID }, true)
                .Index(t => t.NetworkID);

            // default admin user, the password is: dhadmin_#911
            Sql("insert into [User] ([Login], [PasswordHash], [PasswordSalt], [Role], [Status], [LoginAttempts])" +
                " values ('dhadmin', 'QJu4nYuotJ35uhu4ibju8KTCJia+rciXzW3sxEkiB2k=', 'ajFGb7RBuW0uJRzgCqNy8WhE', 0, 0, 0)");
        }
        
        public override void Down()
        {
            // user network
            DropIndex("UserNetwork", new[] { "NetworkID" });
            DropIndex("UserNetwork", new[] { "UserID", "NetworkID" });
            DropForeignKey("UserNetwork", "NetworkID", "Network");
            DropForeignKey("UserNetwork", "UserID", "User");
            DropTable("UserNetwork");

            // user
            DropIndex("User", new[] { "Login" });
            DropTable("User");

            // equipment
            CreateTable(
                "EquipmentType",
                c => new
                {
                    ID = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, maxLength: 128),
                    Capabilities = c.String(),
                })
                .PrimaryKey(t => t.ID);
            AddColumn("Equipment", "EquipmentTypeID", c => c.Int(nullable: false));
            DropColumn("Equipment", "Type");
            CreateIndex("Equipment", "EquipmentTypeID");
            AddForeignKey("Equipment", "EquipmentTypeID", "EquipmentType", "ID");

            // device
            DropColumn("Device", "Key");

            // device class
            DropIndex("DeviceClass", new[] { "Name", "Version" });
            CreateIndex("DeviceClass", "Name", true);
            AlterColumn("DeviceClass", "Version", c => c.String(maxLength: 32));
            DropColumn("DeviceClass", "IsPermanent");

            // network
            DropIndex("Network", new[] { "Key" });
            DropColumn("Network", "Key");
        }
    }
}
