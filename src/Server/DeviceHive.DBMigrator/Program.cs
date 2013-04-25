using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using DeviceHive.Data.Migrations;

namespace DeviceHive.DBMigrator
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                var repositoryAssembly = ConfigurationManager.AppSettings["RepositoryAssembly"];
                if (string.IsNullOrWhiteSpace(repositoryAssembly))
                    throw new InvalidOperationException("Please specify the RepositoryAssembly setting in the configuration file!");

                var repositoryAssemblyObject = Assembly.Load(repositoryAssembly);
                var migratorType = repositoryAssemblyObject.GetTypes().FirstOrDefault(t => typeof(IMigrator).IsAssignableFrom(t));
                if (migratorType == null)
                    throw new InvalidOperationException("Specified assembly does not contain migrator class!");

                var migrator = (IMigrator)Activator.CreateInstance(migratorType);

                Console.WriteLine("Starting DeviceHive database migrator");
                Console.WriteLine("Using repository assembly: " + repositoryAssembly);

                Console.WriteLine("Database version: " + migrator.DatabaseVersion);
                Console.WriteLine("Current version: " + migrator.CurrentVersion);

                if (migrator.CurrentVersion == migrator.DatabaseVersion)
                {
                    Console.WriteLine("No migration is required, exiting");
                    return 0;
                }

                Console.WriteLine("Migrating the database to version: " + migrator.CurrentVersion);

                migrator.Migrate();
                Console.WriteLine("Database migration completed successfully");
                
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
