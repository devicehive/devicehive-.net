using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.Data.Migrations;
using MongoDB.Bson;
using MongoDB.Driver.Builders;

namespace DeviceHive.Data.MongoDB.Migrations
{
    /// <summary>
    /// Represents MongoDB migrator
    /// </summary>
    public class Migrator : IMigrator
    {
        private MongoConnection _connection;
        private List<MigrationBase> _migrations;

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public Migrator()
            : this(new MongoConnection())
        {
        }

        /// <summary>
        /// Specifies a MongoConnection to use
        /// </summary>
        /// <param name="connection">MongoConnection object</param>
        public Migrator(MongoConnection connection)
        {
            _connection = connection;
            _migrations = GetType().Assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(MigrationBase).IsAssignableFrom(t))
                .Select(t => (MigrationBase)Activator.CreateInstance(t))
                .OrderBy(m => m.Version).ToList();

            foreach (var migration in _migrations)
                migration.Initialize(connection);
        }
        #endregion

        #region IMigrator Members

        /// <summary>
        /// Gets the target database version
        /// </summary>
        public string DatabaseVersion
        {
            get { return GetDatabaseVersion(); }
        }

        /// <summary>
        /// Gets the current (latest) version
        /// </summary>
        public string CurrentVersion
        {
            get { return _migrations.Any() ? _migrations.Last().Version : null; }
        }

        /// <summary>
        /// Gets array of all versions
        /// </summary>
        /// <returns>Array of versions</returns>
        public string[] GetAllVersions()
        {
            return _migrations.Select(m => m.Version).ToArray();
        }

        /// <summary>
        /// Migrates the target database to the current version
        /// </summary>
        public void Migrate()
        {
            Migrate(CurrentVersion);
        }

        /// <summary>
        /// Migrates the target database database to the specified version
        /// </summary>
        /// <param name="version">Version to migrate to</param>
        public void Migrate(string version)
        {
            if (version == null)
                throw new ArgumentNullException("version");

            if (!GetAllVersions().Contains(version))
                throw new ArgumentException("Invalid version specified: " + version, "version");

            Migrate(DatabaseVersion, version);
        }
        #endregion

        #region Private Methods

        private string GetDatabaseVersion()
        {
            var versionCollection = _connection.Database.GetCollection("db_version");
            var versionRecord = versionCollection.FindOne();
            return versionRecord == null ? null : versionRecord["value"].AsString;
        }

        private void SetDatabaseVersion(string version)
        {
            var versionCollection = _connection.Database.GetCollection("db_version");
            var versionRecord = versionCollection.FindOne();
            if (versionRecord == null)
            {
                versionCollection.Insert(new BsonDocument("value", version));
            }
            else
            {
                versionCollection.Update(null, Update.Set("value", version));
            }
        }

        private void Migrate(string from, string to)
        {
            if (from == to)
                return;

            var migrationFromIndex = from == null ? -1 : _migrations.IndexOf(_migrations.First(m => m.Version == from));
            var migrationToIndex = _migrations.IndexOf(_migrations.First(m => m.Version == to));

            if (from == null || string.Compare(from, to) < 0)
            {
                var migrations = _migrations.GetRange(migrationFromIndex + 1, migrationToIndex - migrationFromIndex);
                foreach (var migration in migrations)
                {
                    migration.Up();
                    SetDatabaseVersion(migration.Version);
                }
            }
            else
            {
                var migrations = _migrations.GetRange(migrationToIndex + 1, migrationFromIndex - migrationToIndex);
                foreach (var migration in migrations.OrderByDescending(m => m.Version))
                {
                    migration.Down();
                    SetDatabaseVersion(_migrations[_migrations.IndexOf(migration) - 1].Version);
                }
            }
        }
        #endregion
    }
}
