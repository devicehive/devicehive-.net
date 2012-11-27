using System;
using System.Linq;
using DeviceHive.Data.Migrations;

namespace DeviceHive.DBMigrator
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting DeviceHive database migrator...");
                var migrator = new Migrator();

                var pendingMigrations = migrator.GetPendingMigrations();
                if (!pendingMigrations.Any())
                {
                    Console.WriteLine("There is no pending migrations to apply.");
                    return 0;
                }

                Console.WriteLine("Applying the following migrations to the database:");
                foreach (var migration in pendingMigrations)
                {
                    Console.WriteLine("Migration: " + migration);
                }

                migrator.Migrate();
                Console.WriteLine("Database migration completed successfully.");
                
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
                return 1;
            }
        }
    }
}
