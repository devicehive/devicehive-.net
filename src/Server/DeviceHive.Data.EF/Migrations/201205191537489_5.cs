namespace DeviceHive.Data.EF.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class _5 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "Network",
                c => new
                {
                    ID = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, maxLength: 128),
                    Description = c.String(maxLength: 128),
                })
                .PrimaryKey(t => t.ID)
                .Index(t => t.Name, unique: true);

            CreateTable(
                "EquipmentType",
                c => new
                {
                    ID = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, maxLength: 128),
                    Capabilities = c.String(),
                })
                .PrimaryKey(t => t.ID)
                .Index(t => t.Name, unique: true);

            CreateTable(
                "DeviceClass",
                c => new
                {
                    ID = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, maxLength: 128),
                    Version = c.String(maxLength: 32),
                })
                .PrimaryKey(t => t.ID)
                .Index(t => t.Name);

            CreateTable(
                "Equipment",
                c => new
                {
                    ID = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, maxLength: 128),
                    Code = c.String(nullable: false, maxLength: 128),
                    DeviceClassID = c.Int(nullable: false),
                    EquipmentTypeID = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.ID)
                .ForeignKey("DeviceClass", t => t.DeviceClassID, cascadeDelete: true)
                .ForeignKey("EquipmentType", t => t.EquipmentTypeID)
                .Index(t => t.DeviceClassID)
                .Index(t => t.EquipmentTypeID);

            CreateTable(
                "Device",
                c => new
                {
                    ID = c.Int(nullable: false, identity: true),
                    GUID = c.Guid(nullable: false),
                    Name = c.String(nullable: false, maxLength: 128),
                    Status = c.String(maxLength: 128),
                    NetworkID = c.Int(nullable: false),
                    DeviceClassID = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.ID)
                .ForeignKey("Network", t => t.NetworkID, cascadeDelete: true)
                .ForeignKey("DeviceClass", t => t.DeviceClassID, cascadeDelete: true)
                .Index(t => t.NetworkID)
                .Index(t => t.DeviceClassID)
                .Index(t => t.GUID, unique: true);

            CreateTable(
                "DeviceNotification",
                c => new
                {
                    ID = c.Int(nullable: false, identity: true),
                    Timestamp = c.DateTime(nullable: false, storeType: "datetime2"),
                    Notification = c.String(nullable: false, maxLength: 128),
                    Parameters = c.String(),
                    DeviceID = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.ID)
                .ForeignKey("Device", t => t.DeviceID, cascadeDelete: true)
                .Index(t => new { t.DeviceID, t.Timestamp });

            CreateTable(
                "DeviceCommand",
                c => new
                {
                    ID = c.Int(nullable: false, identity: true),
                    Timestamp = c.DateTime(nullable: false, storeType: "datetime2"),
                    Command = c.String(nullable: false, maxLength: 128),
                    Parameters = c.String(),
                    Lifetime = c.Int(),
                    Flags = c.Int(),
                    Status = c.String(maxLength: 128),
                    Result = c.String(maxLength: 1024),
                    DeviceID = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.ID)
                .ForeignKey("Device", t => t.DeviceID, cascadeDelete: true)
                .Index(t => new { t.DeviceID, t.Timestamp });

            CreateTable(
                "DeviceEquipment",
                c => new
                {
                    ID = c.Int(nullable: false, identity: true),
                    Code = c.String(nullable: false, maxLength: 128),
                    Timestamp = c.DateTime(nullable: false, storeType: "datetime2"),
                    Parameters = c.String(),
                    DeviceID = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.ID)
                .ForeignKey("Device", t => t.DeviceID, cascadeDelete: true)
                .Index(t => new { t.DeviceID, t.Code }, unique: true);
        }
        
        public override void Down()
        {
            DropTable("Network");
            DropTable("EquipmentType");
            DropTable("DeviceClass");
            DropIndex("Equipment", new[] { "EquipmentTypeID" });
            DropIndex("Equipment", new[] { "DeviceClassID" });
            DropForeignKey("Equipment", "EquipmentTypeID", "EquipmentType");
            DropForeignKey("Equipment", "DeviceClassID", "DeviceClass");
            DropTable("Equipment");
            DropIndex("Device", new[] { "GUID" });
            DropIndex("Device", new[] { "DeviceClassID" });
            DropIndex("Device", new[] { "NetworkID" });
            DropForeignKey("Device", "DeviceClassID", "DeviceClass");
            DropForeignKey("Device", "NetworkID", "Network");
            DropTable("Device");
            DropIndex("DeviceNotification", new[] { "DeviceID", "Timestamp" });
            DropForeignKey("DeviceNotification", "DeviceID", "Device");
            DropTable("DeviceNotification");
            DropIndex("DeviceCommand", new[] { "DeviceID", "Timestamp" });
            DropForeignKey("DeviceCommand", "DeviceID", "Device");
            DropTable("DeviceCommand");
            DropIndex("DeviceEquipment", new[] { "DeviceID", "Code" });
            DropForeignKey("DeviceEquipment", "DeviceID", "Device");
            DropTable("DeviceEquipment");
        }
    }
}
