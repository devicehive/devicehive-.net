using System;
using System.Linq;
using System.Data.Entity.Migrations;

namespace DeviceHive.Data.EF.Migrations
{
    /// <summary>
    /// Represents a migrator used to apply existing migrations to the database
    /// </summary>
    public class Migrator
    {
        private DbMigrator _migrator;

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public Migrator()
        {
            _migrator = new DbMigrator(new Configuration());
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Gets all migrations that have been applied to the target database
        /// </summary>
        /// <returns>Array of database migration names</returns>
        public string[] GetDatabaseMigrations()
        {
            return _migrator.GetDatabaseMigrations().ToArray();
        }

        /// <summary>
        /// Gets all migrations that are defined in the current version
        /// </summary>
        /// <returns>Array of database migration names</returns>
        public string[] GetLocalMigrations()
        {
            return _migrator.GetLocalMigrations().ToArray();
        }

        /// <summary>
        /// Gets all migrations that have not been applied to the target database
        /// </summary>
        /// <returns>Array of database migration names</returns>
        public string[] GetPendingMigrations()
        {
            return _migrator.GetPendingMigrations().ToArray();
        }

        /// <summary>
        /// Migrates the target database to the current version
        /// </summary>
        public void Migrate()
        {
            _migrator.Update();
        }

        /// <summary>
        /// Migrates the target database to the specified version
        /// </summary>
        /// <param name="targetMigration">The migration name to update</param>
        public void Migrate(string targetMigration)
        {
            _migrator.Update(targetMigration);
        }
        #endregion
    }
}
