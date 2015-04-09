using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Deployment.WindowsInstaller;
using MongoDB.Driver;

namespace DeviceHive.Setup.Actions
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult ChangeConfigJs(Session session)
        {
            session.Log("Begin CustomAction ChangeConfigJs");

            var t = System.IO.File.CreateText("d:\\test.txt");
            if (System.IO.File.Exists(session.GetTargetPath("INSTALLFOLDER") + "devicehive-admin-console\\scripts\\config.js"))
            {
                t.Write(session.GetTargetPath("INSTALLFOLDER") + "devicehive-admin-console\\scripts\\config.js");
            }
            else
            {
                t.Write("No: " + session.GetTargetPath("INSTALLFOLDER") + "devicehive-admin-console\\scripts\\config.js");
            }
            t.Close();

            try
            {
                var s = System.IO.File.CreateText("d:\\db.txt");
                s.Write(session["SQL_SERVER"]);
                s.Write(session["SQL_DATABASE"]);
                s.Write(session["SQL_USER_ID"]);
                s.Write(session["SQL_PASSWORD"]);
                s.Write(session["AUTH_ADMIN_LOGIN"]);
                s.Write(session["AUTH_ADMIN_PASSWORD"]);
                s.Close();

                //var f = 
                System.IO.File.WriteAllText(session.GetTargetPath("INSTALLFOLDER") + "devicehive-admin-console\\scripts\\config.js", "Hello");
                //f.Write("Hello World");
                //f.Close();
            }
            catch (Exception ex)
            {
                var l = System.IO.File.CreateText(@"c:\Temp\DeviceHive.txt");
                l.Write(ex.Message + "  ||| " + ex.StackTrace.ToString());
                l.Close();
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
                View certificates = session.Database.OpenView("DELETE FROM ComboBox WHERE ComboBox.Property='SSL_CERTIFICATE'");
                certificates.Execute();

                certificates = session.Database.OpenView("select * from ComboBox");
                certificates.Execute();

                X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly);

                session.Log("Count: {0}; Name: {1}; Location: {2}", store.Certificates.Count, store.Name, store.Location);
                X509Certificate2Collection certs = store.Certificates;
                var i = 0;
                foreach (var c in certs)
                {
                    i++;
                    session.Log("Subject: {0}; PublicKey: {1}", c.Subject, c.SubjectName.Name);
                    if (c.Subject.Trim() != String.Empty)
                    {
                        var record = session.Database.CreateRecord(4);
                        record.SetString(1, "SSL_CERTIFICATE");
                        record.SetInteger(2, i);
                        record.SetString(3, c.Subject);
                        record.SetString(4, c.Subject);
                        certificates.Modify(ViewModifyMode.InsertTemporary, record);
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
    }
}
