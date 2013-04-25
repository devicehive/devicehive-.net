using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeviceHive.Data.Migrations
{
    /// <summary>
    /// Represents interface for database migrators
    /// </summary>
    public interface IMigrator
    {
        /// <summary>
        /// Gets the target database version
        /// </summary>
        string DatabaseVersion { get; }

        /// <summary>
        /// Gets the current (latest) version
        /// </summary>
        string CurrentVersion { get; }

        /// <summary>
        /// Gets array of all versions
        /// </summary>
        /// <returns>Array of versions</returns>
        string[] GetAllVersions();

        /// <summary>
        /// Migrates the target database to the current version
        /// </summary>
        void Migrate();

        /// <summary>
        /// Migrates the target database database to the specified version
        /// </summary>
        /// <param name="version">Version to migrate to</param>
        void Migrate(string version);
    }
}
