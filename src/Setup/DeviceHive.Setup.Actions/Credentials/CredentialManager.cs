using System;
using System.Security.Cryptography;
using System.Text;

namespace DeviceHive.Setup.Actions.Credentials
{
    public abstract class CredentialManager
    {
        protected const string DEFAULT_ADMIN_LOGIN = "dhadmin";
        protected string ConnectionString { get; private set; }

        #region Constructor

        public CredentialManager(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException("connectionString");

            ConnectionString = connectionString;
        }

        #endregion

        public void Update(string login, string password)
        {
            if (string.IsNullOrEmpty(login))
                throw new ArgumentNullException("login");

            if (string.IsNullOrEmpty(login))
                throw new ArgumentNullException("password");

            string passwordSalt = GetPasswordSalt(password);
            string passwordHash = GetPasswordHash(password, passwordSalt);
            UpdateCredentials(login, passwordHash, passwordSalt);
        }

        private string GetPasswordSalt(string password)
        {
            // generate random 24-characters salt
            var buffer = new byte[18];
            new Random().NextBytes(buffer);
            return Convert.ToBase64String(buffer);
        }

        private string GetPasswordHash(string password, string passwordSalt)
        {
            // calculate password hash
            var buffer = new byte[18];
            buffer = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(passwordSalt + password));
            return Convert.ToBase64String(buffer);
        }

        protected abstract void UpdateCredentials(string login, string passwordHash, string passwordSalt);

        public static CredentialManager GetCreadentialManagerByDatabaseType(string databaseType, string connectionString)
        {
            if (string.IsNullOrEmpty(databaseType))
                throw new ArgumentNullException("databaseType");

            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException("connectionString");

            if ("MS_SQL".Equals(databaseType, StringComparison.InvariantCultureIgnoreCase))
                return new SqlCredentialManager(connectionString);

            if ("MONGO_DB".Equals(databaseType, StringComparison.InvariantCultureIgnoreCase))
                return new MongoDBCredentialManager(connectionString);

            throw new ArgumentException("Invalid database type.");
        }
    }
}
