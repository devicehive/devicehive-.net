using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeviceHive.Data.MongoDB.Migrations
{
    /// <summary>
    /// Represents the base class for migration
    /// </summary>
    public abstract class MigrationBase
    {
        private MigrationAttribute _attribute;

        #region Public Properties

        /// <summary>
        /// Gets migration version
        /// </summary>
        public string Version
        {
            get { return _attribute.Version; }
        }
        #endregion

        #region Protected Properties

        /// <summary>
        /// Gets connection object
        /// </summary>
        protected MongoConnection Connection { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public MigrationBase()
        {
            _attribute = GetType().GetCustomAttributes(typeof(MigrationAttribute), false).Cast<MigrationAttribute>().FirstOrDefault();
            
            if (_attribute == null)
                throw new InvalidOperationException("Migration classes must define MigrationAttribute!");
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes migration with the connection object
        /// </summary>
        /// <param name="connection">MongoConnection object</param>
        public void Initialize(MongoConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");

            Connection = connection;
        }

        /// <summary>
        /// Migrates database up
        /// </summary>
        public abstract void Up();

        /// <summary>
        /// Migrates database down
        /// </summary>
        public abstract void Down();

        #endregion
    }
}
