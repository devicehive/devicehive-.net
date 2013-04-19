using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeviceHive.Data.MongoDB.Migrations
{
    /// <summary>
    /// Represents the attribute used to specify migration version
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MigrationAttribute : Attribute
    {
        #region Public Properties

        /// <summary>
        /// Gets migration version
        /// </summary>
        public string Version { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="version">Migration version</param>
        public MigrationAttribute(string version)
        {
            if (string.IsNullOrEmpty(version))
                throw new ArgumentException("Version is null or empty", "version");

            Version = version;
        }
        #endregion
    }
}
