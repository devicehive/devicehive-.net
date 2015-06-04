using System;
using System.Data;
using System.Data.SqlClient;

namespace DeviceHive.Setup.Actions
{
    public class SqlDatabasePermissionValidator
    {
        private SqlConnection _connection;

        public SqlDatabasePermissionValidator(SqlConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");

            _connection = connection;
        }

        public void Validate(string databaseName, string permissionName)
        {
            if (databaseName == null)
                throw new ArgumentNullException("databaseName");

            if (permissionName == null)
                throw new ArgumentNullException("permissionName");

            string sqlCommand = string.Format("SELECT HAS_PERMS_BY_NAME('{0}', 'DATABASE', '{1}')", databaseName, permissionName);
            IDbCommand command = new SqlCommand(sqlCommand, _connection);
            if (!Convert.ToBoolean(command.ExecuteScalar()))
                throw new Exception(string.Format("Current user does not have permission to create tables in database '{0}'.", databaseName));
        }
    }
}
