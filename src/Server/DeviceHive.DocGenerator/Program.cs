using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Xml;
using DeviceHive.DocGenerator.Templates;
using DeviceHive.API.Models;

namespace DeviceHive.DocGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var doc = GetDocumentation();

                var html = new DeviceHiveAPI { Doc = doc }.TransformText().Trim();
                html = html.Replace("{image-path}/", "");
                File.WriteAllText(@"DeviceHiveAPI.html", html);

                var htmlForDrupal = new PartialApi { Doc = doc }.TransformText().Trim();
                htmlForDrupal = htmlForDrupal.Replace("{image-path}", "<?php print $doc_dir; ?>");
                File.WriteAllText(@"DeviceHiveAPI_ForDrupal.html", htmlForDrupal);


                Console.WriteLine("API documentation has been generated successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
        }

        private static Metadata GetDocumentation()
        {
            var request = (HttpWebRequest)HttpWebRequest.Create("http://localhost/DeviceHive.API/metadata");
            request.Accept = "text/xml";
            var response = request.GetResponse();

            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                using (var xmlReader = new XmlTextReader(reader))
                {
                    return (Metadata)new DataContractSerializer(typeof(Metadata)).ReadObject(xmlReader);
                }
            }
        }
    }
}
