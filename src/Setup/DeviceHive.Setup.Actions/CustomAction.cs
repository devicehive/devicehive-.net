﻿using System;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using DeviceHive.Setup.Actions.Credentials;
using DeviceHive.Setup.Actions.Validation;
using DeviceHive.Setup.Actions.Validation.AuthenticationProvider;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Web.Administration;
using MongoDB.Driver;

namespace DeviceHive.Setup.Actions
{
    public partial class CustomActions
    {
        #region Constants

        private const string PORT_BINDING_PATTERN = "*:{0}:";
        private const string HOST_NAME_BINDING_PATTERN = "*:{0}:{1}";
        private const string ERROR_MESSAGE = "0";

        #endregion

        [CustomAction]
        public static ActionResult ChangeConfigJs(Session session)
        {
            session.Log("Begin CustomAction ChangeConfigJs");

            var configPath = session.GetTargetPath("INSTALL_ADMIN_FOLDER") + "scripts\\config.js";
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("app.config = {");
                sb.AppendLine("    restEndpoint: '/api',");
                sb.AppendLine("    rootUrl: '/admin/',");
                sb.AppendLine("    pushState: false");
                sb.AppendLine("};");

                File.WriteAllText(configPath, sb.ToString());
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

            session["MONGODB_CONNECTION_ESTABLISHED"] = "1";

            try
            {
                string hostName = GetPropertyStringValue(session, "MONGO_HOST");
                if (string.IsNullOrEmpty(hostName))
                {
                    throw new Exception("Host name is empty. Please enter a correct value.");
                }

                string databaseName = GetPropertyStringValue(session, "MONGO_DATABASE");
                if (string.IsNullOrEmpty(databaseName))
                {
                    throw new Exception("Database name is empty. Please enter a correct value.");
                }

                string connectionString = GetPropertyStringValue(session, "DATABASE_CONNECTION_STRING");
                session.Log("Connection string to MongoDB: {0}", connectionString);

                var mongoDb = new MongoClient(connectionString).GetServer();
                var databaseExists = mongoDb.DatabaseExists(databaseName);
                session.Log("Database {0} {1} exist.", databaseName, databaseExists ? "already" : "does not");
            }
            catch (Exception e)
            {
                InitializeMessageBox(session, e.Message, ERROR_MESSAGE);
                session.Log("Error: {0}; {1};", e.Message, e.StackTrace);
                session["MONGODB_CONNECTION_ESTABLISHED"] = "0";
            }

            session.Log("Finish: CheckMongoDbConnection.");

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CheckSqlServerConnection(Session session)
        {
            session.Log("Start: CheckSqlServerConnection.");

            session["SQL_CONNECTION_ESTABLISHED"] = "1";

            try
            {
                string serverName = GetPropertyStringValue(session, "SQL_SERVER");
                if (string.IsNullOrEmpty(serverName))
                {
                    throw new Exception("Server name is empty. Please enter a correct value.");
                }

                string database = GetPropertyStringValue(session, "SQL_DATABASE");
                if (string.IsNullOrEmpty(database))
                {
                    throw new Exception("Database name is empty. Please enter a correct value.");
                }

                string userName = GetPropertyStringValue(session, "SQL_USER_ID");
                if (string.IsNullOrEmpty(userName))
                {
                    throw new Exception("Login is empty. Please enter a correct value.");
                }

                string password = GetPropertyStringValue(session, "SQL_PASSWORD");
                if (string.IsNullOrEmpty(password))
                {
                    throw new Exception("Password is empty. Please enter a correct value.");
                }

                string connectionString = GetPropertyStringValue(session, "DATABASE_CONNECTION_STRING");
                session.Log("Connection string to SQL Server: {0}", connectionString);

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlDatabaseValidator databaseValidator = new SqlDatabaseValidator(connection);
                    databaseValidator.Validate(database);

                    SqlDatabasePermissionValidator databasePermissionValidator = new SqlDatabasePermissionValidator(connection);
                    databasePermissionValidator.Validate(database, "CREATE TABLE");
                }
            }
            catch (Exception e)
            {
                InitializeMessageBox(session, e.Message, ERROR_MESSAGE);
                session.Log("Error: {0}; {1};", e.Message, e.StackTrace);
                session["SQL_CONNECTION_ESTABLISHED"] = "0";
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

                CreateComboBoxRecordItem(certView, numRows++, "SSL_CERTIFICATE", "Not Selected", "Not Selected");

                foreach (var certificate in certificates)
                {
                    session.Log("Subject: {0}; PublicKey: {1}", certificate.Subject, certificate.SubjectName.Name);
                    if (certificate.Subject.Trim() != String.Empty)
                    {
                        CreateComboBoxRecordItem(certView, numRows++, "SSL_CERTIFICATE", GetCertificateFriendlyName(certificate), certificate.Subject);
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
        public static ActionResult CheckSiteBinding(Session session)
        {
            session.Log("Start: CheckSiteBinding.");

            session["SITE_BINDING_IS_VALID"] = "1";

            try
            {
                string webSiteName = GetPropertyStringValue(session, "WEB_SITE_NAME", true);
                string portNumber = GetPropertyStringValue(session, "WEB_SITE_PORT_NUMBER");
                string hostName = GetPropertyStringValue(session, "DOMAIN_NAME");

                SiteBindingValidator siteBindingValidator = new SiteBindingValidator();
                siteBindingValidator.Validate(webSiteName, hostName, portNumber);
            }
            catch (Exception e)
            {
                InitializeMessageBox(session, e.Message, ERROR_MESSAGE);
                session["SITE_BINDING_IS_VALID"] = "0";
            }
            finally
            {
                session.Log("Finish: CheckSiteBinding.");
            }
            return ActionResult.Success;
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
            session["AUTHENTICATION_SETTINGS_IS_VALID"] = "1";
            try
            {
                AuthenticationValidator authenticationValidator = new GoogleAuthenticationProviderValidator();

                string clientId = GetPropertyStringValue(session, "AUTH_GOOGLE_CLIENT_ID");
                string clientSecret = GetPropertyStringValue(session, "AUTH_GOOGLE_CLIENT_SECRET");

                authenticationValidator.Validate(clientId, clientSecret);
            }
            catch (Exception e)
            {
                InitializeMessageBox(session, e.Message, ERROR_MESSAGE);
                session["AUTHENTICATION_SETTINGS_IS_VALID"] = "0";
            }
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CheckFacebookAuthenticationSettings(Session session)
        {
            session["AUTHENTICATION_SETTINGS_IS_VALID"] = "1";
            try
            {
                AuthenticationValidator authenticationValidator = new FacebookAuthenticationValidator();

                string clientId = GetPropertyStringValue(session, "AUTH_FACEBOOK_CLIENT_ID");
                string clientSecret = GetPropertyStringValue(session, "AUTH_FACEBOOK_CLIENT_SECRET");

                authenticationValidator.Validate(clientId, clientSecret);
            }
            catch (Exception e)
            {
                InitializeMessageBox(session, e.Message, ERROR_MESSAGE);
                session["AUTHENTICATION_SETTINGS_IS_VALID"] = "0";
            }

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CheckGithubAuthenticationSettings(Session session)
        {
            session["AUTHENTICATION_SETTINGS_IS_VALID"] = "1";
            try
            {
                AuthenticationValidator authenticationValidator = new GithubAuthenticationValidator();

                string clientId = GetPropertyStringValue(session, "AUTH_GITHUB_CLIENT_ID");
                string clientSecret = GetPropertyStringValue(session, "AUTH_GITHUB_CLIENT_SECRET");

                authenticationValidator.Validate(clientId, clientSecret);
            }
            catch (Exception e)
            {
                InitializeMessageBox(session, e.Message, ERROR_MESSAGE);
                session["AUTHENTICATION_SETTINGS_IS_VALID"] = "0";
            }

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CheckAdministratorCredentials(Session session)
        {
            session["ADMINISTRATOR_CREDENTIALS_IS_VALID"] = "1";

            try
            {
                string adminLogin = GetPropertyStringValue(session, "AUTH_ADMIN_LOGIN");
                string adminPasssword = GetPropertyStringValue(session, "AUTH_ADMIN_PASSWORD");

                AdministratorCredentialsValidator validator = new AdministratorCredentialsValidator();
                validator.Validate(adminLogin, adminPasssword);
            }
            catch (Exception e)
            {
                InitializeMessageBox(session, e.Message, ERROR_MESSAGE);
                session["ADMINISTRATOR_CREDENTIALS_IS_VALID"] = "0";
            }
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult UpdateAdministratorCredentials(Session session)
        {
            try
            {
                string databaseType = GetPropertyStringValue(session, "DATABASE_TYPE");
                string connectionString = GetPropertyStringValue(session, "DATABASE_CONNECTION_STRING");
                CredentialManager credentialManager = CredentialManager.GetCreadentialManagerByDatabaseType(databaseType, connectionString);

                string adminLogin = GetPropertyStringValue(session, "AUTH_ADMIN_LOGIN");
                string adminPasssword = GetPropertyStringValue(session, "AUTH_ADMIN_PASSWORD");
                credentialManager.Update(adminLogin, adminPasssword);
            }
            catch (Exception e)
            {
                session.Log("Error: {0}; {1};", e.Message, e.StackTrace);
            }
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
    }
}