using System;
using System.IO;
using DeviceHive.DocGenerator.Templates;

namespace DeviceHive.DocGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var apiMetadata = new ApiMetadataGenerator().Generate();

                var html = new DeviceHiveAPI { ApiMetadata = apiMetadata }.TransformText().Trim();
                html = html.Replace("{image-path}/", "");
                File.WriteAllText(@"DeviceHiveAPI.html", html);

                var htmlForDrupal = new PartialApi { ApiMetadata = apiMetadata }.TransformText().Trim();
                htmlForDrupal = htmlForDrupal.Replace("{image-path}", "<?php print $doc_dir; ?>");
                File.WriteAllText(@"DeviceHiveAPI_ForDrupal.html", htmlForDrupal);

                Console.WriteLine("API documentation has been generated successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
        }
    }
}
