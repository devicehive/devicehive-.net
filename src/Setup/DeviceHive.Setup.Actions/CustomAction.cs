using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Microsoft.Deployment.WindowsInstaller;
using MongoDB.Driver;
using Microsoft.Web.Administration;

namespace DeviceHive.Setup.Actions
{
    public class CustomActions
    {
        #region Constants

        private const string PORT_BINDING_PATTERN = "*:{0}:";
        private const string HOST_NAME_BINDING_PATTERN = "*:{0}:{1}";
        private const string ERROR_MESSAGE = "0";

        #endregion

        /// <summary>
        /// Check that process is being run as an administrator
        /// </summary>
        /// <returns></returns>
        [CustomAction]
        public static ActionResult CheckRunAsAdministrator(Session session)
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            int value = Convert.ToInt32(principal.IsInRole(WindowsBuiltInRole.Administrator));
            session["IS_ADMIN"] = value.ToString(CultureInfo.InvariantCulture);

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult ChangeConfigJs(Session session)
        {
            session.Log("Begin CustomAction ChangeConfigJs");

            var configPath = session.GetTargetPath("INSTALLFOLDER") + "admin\\scripts\\config.js";

            session.Log("Config.js on path {0} exists: {1} ", configPath, System.IO.File.Exists(configPath));

            try
            {
                //TODO: Implement changing config.js file
            }
            catch (Exception e)
            {
                session.Log("Error: {0}; {1};", e.Message, e.StackTrace);
            }
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CheckMongoDbConnection(Session session)
        {
            session.Log("Start: CheckMongoDbConnection.");

            session["MONGODB_CONNECTION_ESTABLISHED"] = "0";

            string hostName = GetPropertyStringValue(session, "MONGO_HOST");
            if (string.IsNullOrEmpty(hostName))
            {
                InitializeMessageBox(session, "Host name is empty. Please enter a correct value.", ERROR_MESSAGE);
                return ActionResult.Success;
            }

            string database = GetPropertyStringValue(session, "MONGO_DATABASE");
            if (string.IsNullOrEmpty(database))
            {
                InitializeMessageBox(session, "Database name is empty. Please enter a correct value.", ERROR_MESSAGE);
                return ActionResult.Success;
            }

            string login = GetPropertyStringValue(session, "MONGO_LOGIN");
            string password = GetPropertyStringValue(session, "MONGO_PASSWORD");

            var connectionString = string.Format("mongodb://{0}/{1}", hostName, database);
            if (!string.IsNullOrWhiteSpace(login) && !string.IsNullOrWhiteSpace(password))
            {
                connectionString = string.Format("mongodb://{0}:{1}@{2}/{3}", login, password, hostName, database);
            }

            session.Log("Connection string to MongoDB: {0}", connectionString);

            try
            {
                var mongoDb = new MongoClient(connectionString).GetServer();
                var databaseExists = mongoDb.DatabaseExists(database);
                session.Log("Database {0} {1} exist.", database, databaseExists ? "already" : "does not");
                session["MONGODB_CONNECTION_ESTABLISHED"] = "1";
            }
            catch (MongoConnectionException e)
            {
                InitializeMessageBox(session, e.Message, ERROR_MESSAGE);
                session.Log("Error: {0}; {1};", e.Message, e.StackTrace);
            }

            session.Log("Finish: CheckMongoDbConnection.");

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CheckSqlServerConnection(Session session)
        {
            session.Log("Start: CheckSqlServerConnection.");

            session["SQL_CONNECTION_ESTABLISHED"] = "0";

            string serverName = GetPropertyStringValue(session, "SQL_SERVER");
            if (string.IsNullOrEmpty(serverName))
            {
                InitializeMessageBox(session, "Server name is empty. Please enter a correct value.", ERROR_MESSAGE);
                return ActionResult.Success;
            }

            string database = GetPropertyStringValue(session, "SQL_DATABASE");
            if (string.IsNullOrEmpty(database))
            {
                InitializeMessageBox(session, "Database name is empty. Please enter a correct value.", ERROR_MESSAGE);
                return ActionResult.Success;
            }
            string userName = GetPropertyStringValue(session, "SQL_USER_ID");
            if (string.IsNullOrEmpty(userName))
            {
                InitializeMessageBox(session, "User login is empty. Please enter a correct value.", ERROR_MESSAGE);
                return ActionResult.Success;
            }

            string password = GetPropertyStringValue(session, "SQL_PASSWORD");
            if (string.IsNullOrEmpty(password))
            {
                InitializeMessageBox(session, "Password is empty. Please enter a correct value.", ERROR_MESSAGE);
                return ActionResult.Success;
            }

            try
            {
                string connectionString = string.Format("Data Source={0};UID={1};Password={2};", serverName, userName, password);
                session.Log("Connection string to SQL Server: {0}", connectionString);

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    if (!IsDatabaseExists(connection, database))
                    {
                        string errorMessage = string.Format("Database '{0}' does not exist. Please enter a correct database name.", database);
                        InitializeMessageBox(session, errorMessage, ERROR_MESSAGE);
                        return ActionResult.Success;
                    }

                    if (!HasDatabasePermission(connection, database, "CREATE TABLE"))
                    {
                        string errorMessage = string.Format("User '{0}' does not have permission to create tables in database '{1}'.", userName, database);
                        InitializeMessageBox(session, errorMessage, ERROR_MESSAGE);
                        return ActionResult.Success;
                    }
                    session["SQL_CONNECTION_ESTABLISHED"] = "1";
                }
            }
            catch (SqlException e)
            {
                InitializeMessageBox(session, e.Message, ERROR_MESSAGE);
                session.Log("Error: {0}; {1};", e.Message, e.StackTrace);
            }

            session.Log("Finish: CheckSqlServerConnection.");

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult GetCertificates(Session session)
        {
            session.Log("Start: GetCertificates.");

            session["SSL_CERTIFICATE"] = "Not Selected";

            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);

            try
            {
                store.Open(OpenFlags.ReadOnly);

                session.Log("Count: {0}; Name: {1}; Location: {2}", store.Certificates.Count, store.Name, store.Location);
                X509Certificate2Collection certificates = store.Certificates;
                if (certificates.Count == 0)
                {
                    session["SSL_CERTIFICATE_STORAGE_EMPTY"] = "true";
                    session.Log("Certificate storage is empty.");

                    return ActionResult.Success;
                }

                View certView = session.Database.OpenView("DELETE FROM ComboBox WHERE ComboBox.Property='SSL_CERTIFICATE'");
                certView.Execute();
                certView = session.Database.OpenView("select * from ComboBox");
                certView.Execute();

                int numRows = 0;

                CreateRecordItem(certView, numRows++, "SSL_CERTIFICATE", "Not Selected", "Not Selected");

                foreach (var certificate in certificates)
                {
                    session.Log("Subject: {0}; PublicKey: {1}", certificate.Subject, certificate.SubjectName.Name);
                    if (certificate.Subject.Trim() != String.Empty)
                    {
                        CreateRecordItem(certView, numRows++, "SSL_CERTIFICATE", GetCertificateFriendlyName(certificate), certificate.Subject);
                    }
                }
            }
            catch (Exception e)
            {
                session.Log("Error: {0}; {1};", e.Message, e.StackTrace);
                return ActionResult.Failure;
            }
            finally
            {
                store.Close();
                session.Log("Finish: GetCertificates.");
            }

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult InitializeSiteId(Session session)
        {
            session.Log("Start: InitializeSiteId.");

            ActionResult result = ActionResult.Success;
            try
            {
                string webSiteName = GetPropertyStringValue(session, "WEB_SITE_NAME", true);
                using (var serverManager = new ServerManager())
                {
                    Site site = serverManager.Sites.SingleOrDefault(s => s.Name == webSiteName);
                    long sideId = (site != null) ? site.Id : serverManager.Sites.Max(s => s.Id) + 1;
                    session["SITE_ID"] = sideId.ToString(CultureInfo.InvariantCulture);

                    session.Log("Web Site Name {0} site id {1}", session["WEB_SITE_NAME"], session["SITE_ID"]);
                }
            }
            catch (Exception e)
            {
                result = ActionResult.Failure;
                session.Log("Error: {0}; {1};", e.Message, e.StackTrace);
            }
            finally
            {
                session.Log("Finish: InitializeSiteId.");
            }
            return result;
        }

        [CustomAction]
        public static ActionResult CheckBindingPort(Session session)
        {
            session.Log("Start: CheckBindingPort.");

            session["PORT_NUMBER_IS_VALID"] = "1";

            ActionResult result = ActionResult.Success;

            try
            {
                string webSiteName = GetPropertyStringValue(session, "WEB_SITE_NAME", true);
                string portNumber = GetPropertyStringValue(session, "WEB_SITE_PORT_NUMBER");
                if (!IsPort(portNumber))
                {
                    InitializeMessageBox(session, "Port Number is invalid. Please enter a correct value.", ERROR_MESSAGE);

                    session["PORT_NUMBER_IS_VALID"] = "0";
                    return ActionResult.Success;
                }

                using (var serverManager = new ServerManager())
                {
                    foreach (var site in serverManager.Sites.Where(s => s.Name != webSiteName && s.State == ObjectState.Started))
                    {
                        foreach (var binding in site.Bindings)
                        {
                            if (binding.EndPoint.Port.ToString(CultureInfo.InvariantCulture) == portNumber)
                            {
                                string message = string.Format("Current Port Number {0} already used by {1}.", binding.EndPoint.Port, site.Name);
                                InitializeMessageBox(session, message, ERROR_MESSAGE);

                                session["PORT_NUMBER_IS_VALID"] = "0";
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                result = ActionResult.Failure;
                session.Log("Error: {0}; {1};", e.Message, e.StackTrace);
            }
            finally
            {
                session.Log("Finish: CheckBindingPort.");
            }
            return result;
        }

        [CustomAction]
        public static ActionResult UpdateBinding(Session session)
        {
            session.Log("Start UpdateHttpBinding.");

            ActionResult result = ActionResult.Success;
            try
            {
                string webSiteName = GetPropertyStringValue(session, "WEB_SITE_NAME", true);

                using (var serverManager = new ServerManager())
                {
                    Site site = serverManager.Sites.SingleOrDefault(s => s.Name == webSiteName);
                    if (site == null)
                    {
                        throw new Exception(string.Format("Web Site with name {0} does not exist.", webSiteName));
                    }

                    site.Bindings.Clear();

                    //Update Http Binding
                    UpdateHttpBinding(serverManager, site, session);

                    //Update Https Binding
                    UpdateHttpsBinding(serverManager, site, session);

                    serverManager.CommitChanges();
                }
            }
            catch (Exception e)
            {
                result = ActionResult.Failure;
                session.Log("Error: {0}; {1};", e.Message, e.StackTrace);
            }
            finally
            {
                session.Log("End UpdateHttpBinding.");
            }
            return result;
        }

        [CustomAction]
        public static ActionResult CheckGoogleAuthenticationSettings(Session session)
        {
            CheckAuthenticationSettings(session, "Google", "AUTH_GOOGLE_CLIENT_ID", "AUTH_GOOGLE_CLIENT_SECRET");
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CheckFacebookAuthenticationSettings(Session session)
        {
            CheckAuthenticationSettings(session, "Facebook", "AUTH_FACEBOOK_CLIENT_ID", "AUTH_FACEBOOK_CLIENT_SECRET");
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CheckGithubAuthenticationSettings(Session session)
        {
            CheckAuthenticationSettings(session, "Github", "AUTH_GITHUB_CLIENT_ID", "AUTH_GITHUB_CLIENT_SECRET");
            return ActionResult.Success;
        }

        private static void UpdateHttpBinding(ServerManager serverManager, Site site, Session session)
        {
            string portNumber = GetPropertyStringValue(session, "PORT_NUMBER", true);
            string hostName = GetPropertyStringValue(session, "DOMAIN_NAME");

            string bindingInformation = CreateBindingInformation(portNumber, hostName);
            site.Bindings.Add(bindingInformation, "http");
        }

        private static void UpdateHttpsBinding(ServerManager serverManager, Site site, Session session)
        {
            bool sslEnabled = GetPropertyBoolValue(session, "SSL_ENABLED");
            bool sslCertificateStorageEmpty = GetPropertyBoolValue(session, "SSL_CERTIFICATE_STORAGE_EMPTY");

            if (!sslEnabled || sslCertificateStorageEmpty)
            {
                return;
            }

            string portNumber = GetPropertyStringValue(session, "SSL_PORT_NUMBER", true);
            string hostName = GetPropertyStringValue(session, "DOMAIN_NAME");

            string bindingInformation = CreateBindingInformation(portNumber, hostName);

            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            try
            {
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);

                string sslCertificate = GetPropertyStringValue(session, "SSL_CERTIFICATE", true);
                var certificate = store.Certificates.OfType<X509Certificate2>().FirstOrDefault(c => c.Subject == sslCertificate);
                if (certificate == null)
                {
                    throw new Exception(string.Format("Could not find a certificate with Subject Name:{0}.", sslCertificate));
                }

                if (site.Bindings.All(b => b.BindingInformation != bindingInformation))
                {
                    session.Log("Certificate - Friendly Name: {0}. Thumbprint {1}", certificate.FriendlyName, certificate.Thumbprint);

                    site.Bindings.Add(bindingInformation, certificate.GetCertHash(), store.Name);

                    bool sslRequired = GetPropertyBoolValue(session, "SSL_REQUIRED");
                    if (sslRequired)
                    {
                        Configuration config = serverManager.GetApplicationHostConfiguration();
                        ConfigurationSection accessSection = config.GetSection("system.webServer/security/access", site.Name);
                        accessSection["sslFlags"] = "Ssl, SslNegotiateCert";
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                store.Close();
            }
        }

        private static bool IsPort(string portNumber)
        {
            if (string.IsNullOrEmpty(portNumber))
                return false;

            Regex numeric = new Regex(@"^[1-9]\d{1,5}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (!numeric.IsMatch(portNumber))
                return false;

            int value;
            if (int.TryParse(portNumber, out value))
            {
                return value >= 1 && value < 65536;
            }
            return false;
        }

        private static bool GetPropertyBoolValue(Session session, string propertyName, bool required = false)
        {
            string propertyValue = session[propertyName];
            if (string.IsNullOrEmpty(propertyValue))
            {
                string errorMessage = string.Format("{0} property has not been set up.", propertyName);
                session.Log(errorMessage);
                if (required)
                {
                    throw new ArgumentException(errorMessage);
                }
                return false;
            }

            bool result;
            if (!bool.TryParse(propertyValue, out result))
            {
                throw new ArgumentException(string.Format("{0} property has wrong type value.", propertyName));
            }
            return result;
        }

        private static string GetPropertyStringValue(Session session, string propertyName, bool required = false)
        {
            string propertyValue = session[propertyName];
            if (string.IsNullOrEmpty(propertyValue))
            {
                string errorMessage = string.Format("{0} property has not been set up.", propertyName);
                session.Log(errorMessage);
                if (required)
                {
                    throw new ArgumentException(errorMessage);
                }
                return string.Empty;
            }
            return propertyValue.Trim();
        }

        private static void CreateRecordItem(View view, int numRows, string propertyName, string text, string value)
        {
            Record record = new Record(4);
            record.SetString(1, propertyName);
            record.SetInteger(2, numRows);
            record.SetString(3, value);
            record.SetString(4, text);
            view.Modify(ViewModifyMode.InsertTemporary, record);
        }

        private static string GetCertificateFriendlyName(X509Certificate2 certificate)
        {
            if (!string.IsNullOrEmpty(certificate.FriendlyName))
                return certificate.FriendlyName;

            string[] array = certificate.Subject.Split('=');
            return array.Length == 2 ? array[1] : certificate.FriendlyName;
        }

        private static string CreateBindingInformation(string portNumber, string hostName)
        {
            string bindingInformation = string.Format(PORT_BINDING_PATTERN, portNumber);
            if (!string.IsNullOrEmpty(hostName))
            {
                bindingInformation = string.Format(HOST_NAME_BINDING_PATTERN, portNumber, hostName);
            }
            return bindingInformation;
        }

        private static void InitializeMessageBox(Session session, string message, string messageType)
        {
            session["MESSAGE_TEXT"] = message;
            session["MESSAGE_TYPE"] = messageType;
        }

        private static void CheckAuthenticationSettings(Session session, string providerName, string clientIdPropertyName, string clientSecretPropertyName)
        {
            session["AUTHENTICATION_SETTINGS_IS_VALID"] = "0";

            try
            {
                string clientIdValue = GetPropertyStringValue(session, clientIdPropertyName);
                if (string.IsNullOrEmpty(clientIdValue))
                {
                    throw new Exception(string.Format("Client Id for Authentication {0} provider is empty. Please enter a correct value.", providerName));
                }
                string clientSecretValue = GetPropertyStringValue(session, clientSecretPropertyName);
                if (string.IsNullOrEmpty(clientSecretValue))
                {
                    throw new Exception(string.Format("Client Secret for Authentication {0} provider is empty. Please enter a correct value.", providerName));
                }
                session["AUTHENTICATION_SETTINGS_IS_VALID"] = "1";
            }
            catch (Exception e)
            {
                InitializeMessageBox(session, e.Message, ERROR_MESSAGE);
            }
        }

        private static bool IsDatabaseExists(SqlConnection connection, string database)
        {
            string sqlCommand = string.Format("SELECT CASE WHEN db_id('{0}') is null THEN 0 ELSE 1 END", database);
            IDbCommand command = new SqlCommand(sqlCommand, connection);
            return Convert.ToBoolean(command.ExecuteScalar());
        }

        private static bool HasDatabasePermission(SqlConnection connection, string database, string permissionName)
        {
            string sqlCommand = string.Format("SELECT HAS_PERMS_BY_NAME('{0}', 'DATABASE', '{1}')", database, permissionName);
            IDbCommand command = new SqlCommand(sqlCommand, connection);
            return Convert.ToBoolean(command.ExecuteScalar());
        }
    }
}