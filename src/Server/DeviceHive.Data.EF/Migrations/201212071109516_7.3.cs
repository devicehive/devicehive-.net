namespace DeviceHive.Data.EF.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class _73 : DbMigration
    {
        public override void Up()
        {
            Sql(@"
                if ((select [length] from syscolumns where id = OBJECT_ID('DeviceCommand') and name = 'Result') = 2048)
                begin
                    alter table DeviceCommand
                    alter column Result nvarchar(max)

                    update DeviceCommand
                    set Result = '""' + replace(replace(Result, '\', '\\'), '""', '\""') + '""'
                    where Result is not null
                end
                ");
        }
        
        public override void Down()
        {
            // keep result as nvarchar(max), should not be a problem
        }
    }
}
