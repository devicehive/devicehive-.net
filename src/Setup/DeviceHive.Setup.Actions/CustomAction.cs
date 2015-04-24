using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using Microsoft.Deployment.WindowsInstaller;
using MongoDB.Driver;
using Microsoft.Web.Administration;

namespace DeviceHive.Setup.Actions
{
    public class CustomActions
    {
        private const string Protocol = "https";
        private const string BindingPattern = "*:{0}:";

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
                var message = "Error: " + e.Message + "; " + e.StackTrace + "; ";
                session.Log(message);
            }
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult CheckMongoDbConnection(Session session)
        {
            var host = session["MONGO_HOST"];
            var database = session["MONGO_DATABASE"];

            if (host == null || database == null)
            {
                session["MONGODB_CONNECTION_ESTABLISHED"] = "0";
                return ActionResult.Success;
            }

            var connectionString = "mongodb://" + host + "/" + database;
            session.Log("Connection string to MongoDB: {0}", connectionString);

            try
            {
                var mongoDb = new MongoClient(connectionString).GetServer();
                var databaseExists = mongoDb.DatabaseExists(database);
                session["MONGODB_CONNECTION_ESTABLISHED"] = databaseExists ? "1" : "0";
            }
            catch (Exception e)
            {
                var message = "Error: " + e.Message + "; " + e.StackTrace + "; ";
                session.Log(message);
            }

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult GetCertificates(Session session)
        {
            session.Log("Start: GetCertificates; ");
            try
            {
                View certView = session.Database.OpenView("DELETE FROM ComboBox WHERE ComboBox.Property='SSL_CERTIFICATE'");
                certView.Execute();

                certView = session.Database.OpenView("select * from ComboBox");
                certView.Execute();

                X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly);

                session.Log("Count: {0}; Name: {1}; Location: {2}", store.Certificates.Count, store.Name, store.Location);
                X509Certificate2Collection certificates = store.Certificates;
                var i = 0;
                foreach (var certificate in certificates)
                {
                    i++;
                    session.Log("Subject: {0}; PublicKey: {1}", certificate.Subject, certificate.SubjectName.Name);
                    if (certificate.Subject.Trim() != String.Empty)
                    {
                        var record = session.Database.CreateRecord(4);
                        record.SetString(1, "SSL_CERTIFICATE");
                        record.SetInteger(2, i);
                        record.SetString(3, certificate.Subject);
                        record.SetString(4, certificate.Subject);
                        certView.Modify(ViewModifyMode.InsertTemporary, record);
                    }
                }
            }
            catch (Exception e)
            {
                var message = "Error: " + e.Message + "; " + e.StackTrace + "; ";
                session.Log(message);
                return ActionResult.Failure;
            }
            session.Log("Finish: GetCertificates.");

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult UpdateBinding(Session session)
        {
            var selectedCertificate = session["SSL_CERTIFICATE"];

            ActionResult result = ActionResult.Failure;
            session.Log("Start UpdateBinding.");
            if (CheckRunAsAdministrator())
            {
                bool outcome = UpdateBinding("Default Web Site",
                                            Protocol,
                                            string.Format(BindingPattern, session["SSL_WEB_PORT_NUMBER"]),
                                            selectedCertificate,
                                            session);
                if (outcome) { result = ActionResult.Success; }
                session.Log("End UpdateBinding.");
                return result;
            }
            session.Log("Not running with elevated permissions.STOP");
            session.DoAction("NotElevated");
            return result;
        }

        private static bool UpdateBinding(string sitename, string protocol, string port, string certSubject, Session session)
        {
            bool result = false;
            session.Log(string.Format("Binding info (Port) {0}.", port));
            session.Log(string.Format("Certificate Subject {0}.", certSubject));

            using (var serverManager = new ServerManager())
            {
                Site site = serverManager.Sites.SingleOrDefault(s => s.Name == sitename);

                X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);

                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);

                var certificate = store.Certificates.OfType<X509Certificate2>().FirstOrDefault(c => c.Subject == certSubject);

                if (certificate != null)
                {
                    if (!site.Bindings.Any(b => b.BindingInformation == port))
                    {
                        session.Log(string.Format("Certificate - Friendly Name: {0}. Thumbprint {1}", certificate.FriendlyName, certificate.Thumbprint));
                        site.Bindings.Add(port, certificate.GetCertHash(), store.Name);
                        serverManager.CommitChanges();
                    }
                    result = true;
                }

                session.Log(string.Format("Could not find a certificate with Subject Name:{0}.", certSubject));

                store.Close();

            }
            return result;
        }

        /// <summary>
        /// Check that process is being run as an administrator
        /// </summary>
        /// <returns></returns>
        private static bool CheckRunAsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}

