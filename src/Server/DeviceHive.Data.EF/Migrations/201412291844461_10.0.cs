namespace DeviceHive.Data.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _100 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.User", "FacebookLogin", c => c.String(maxLength: 64));
            AddColumn("dbo.User", "GoogleLogin", c => c.String(maxLength: 64));
            AddColumn("dbo.User", "GithubLogin", c => c.String(maxLength: 64));
            AlterColumn("dbo.User", "PasswordHash", c => c.String(maxLength: 48));
            AlterColumn("dbo.User", "PasswordSalt", c => c.String(maxLength: 24));

            Sql("CREATE UNIQUE NONCLUSTERED INDEX [IX_FacebookLogin] ON [User] ( [FacebookLogin] ASC ) WHERE [FacebookLogin] IS NOT NULL");
            Sql("CREATE UNIQUE NONCLUSTERED INDEX [IX_GoogleLogin] ON [User] ( [GoogleLogin] ASC ) WHERE [GoogleLogin] IS NOT NULL");
            Sql("CREATE UNIQUE NONCLUSTERED INDEX [IX_GithubLogin] ON [User] ( [GithubLogin] ASC ) WHERE [GithubLogin] IS NOT NULL");
        }
        
        public override void Down()
        {
            DropIndex("dbo.User", "IX_FacebookLogin");
            DropIndex("dbo.User", "IX_GoogleLogin");
            DropIndex("dbo.User", "IX_GithubLogin");

            AlterColumn("dbo.User", "PasswordSalt", c => c.String(nullable: false, maxLength: 24));
            AlterColumn("dbo.User", "PasswordHash", c => c.String(nullable: false, maxLength: 48));
            DropColumn("dbo.User", "GithubLogin");
            DropColumn("dbo.User", "GoogleLogin");
            DropColumn("dbo.User", "FacebookLogin");
        }
    }
}
