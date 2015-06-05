using System;
using System.Data;
using System.Data.SqlClient;

namespace DeviceHive.Setup.Actions.Credentials
{
    public class SqlCredentialManager : CredentialManager
    {
        private const string COMMAND_TEXT = "UPDATE [User] SET [Login] = '{1}', [PasswordHash] = '{2}', [PasswordSalt] = '{3}' WHERE [Login] = '{0}'";

        #region Constructor
        public SqlCredentialManager(string connectionString)
            : base(connectionString)
        {
        }

        #endregion

        protected override void UpdateCredentials(string login, string passwordHash, string passwordSalt)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                string commandText = string.Format(COMMAND_TEXT, DEFAULT_ADMIN_LOGIN, login, passwordHash, passwordSalt);
                SqlCommand command = new SqlCommand(commandText, connection);
                int rowCount = command.ExecuteNonQuery();
                if (rowCount == 0)
                {
                    throw new Exception("User not found!");
                }
            }
        }
    }
}
