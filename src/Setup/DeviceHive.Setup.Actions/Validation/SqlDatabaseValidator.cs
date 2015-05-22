using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;

namespace DeviceHive.Setup.Actions
{
    class SqlDatabaseValidator
    {
        private SqlConnection _connection;

        public SqlDatabaseValidator(SqlConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");

            _connection = connection;
        }

        public void Validate(string databaseName)
        {
            if (databaseName == null)
                throw new ArgumentNullException("databaseName");

            string sqlCommand = string.Format("SELECT CASE WHEN db_id('{0}') is null THEN 0 ELSE 1 END", databaseName);
            IDbCommand command = new SqlCommand(sqlCommand, _connection);
            if (!Convert.ToBoolean(command.ExecuteScalar()))
                throw new Exception(string.Format("Database '{0}' does not exist. Please enter a correct database name.", databaseName));
        }
    }
}
