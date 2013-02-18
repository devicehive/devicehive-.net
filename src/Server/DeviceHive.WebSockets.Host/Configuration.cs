using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Xml.Serialization;

namespace DeviceHive.WebSockets.Host
{
    /// <summary>
    /// Web sockets host service configuration section handler
    /// </summary>
    public class ServiceConfigurationSection : ConfigurationSection
    {
        /// <summary>
        /// Gets or sets web sockets listen URL
        /// </summary>
        [ConfigurationProperty("listenUrl", IsRequired = true)]
        public string ListenUrl
        {
            get { return (string) base["listenUrl"]; }
            set { base["listenUrl"] = value; }
        }

        /// <summary>
        /// Gets or sets SSL certificate serial number
        /// </summary>
        [ConfigurationProperty("certificateSerialNumber")]
        public string CertificateSerialNumber
        {
            get { return (string) base["certificateSerialNumber"]; }
            set { base["certificateSerialNumber"] = value; }
        }

        /// <summary>
        /// Gets or sets runtime configuration file path
        /// </summary>
        [ConfigurationProperty("runtimeConfigPath", DefaultValue = "DeviceHive.WebSockets.Host.Runtime.config")]
        public string RuntimeConfigPath
        {
            get { return (string) base["runtimeConfigPath"]; }
            set { base["runtimeConfigPath"] = value; }
        }

        /// <summary>
        /// Gets or sets tempalte for host pipe name
        /// </summary>
        [ConfigurationProperty("hostPipeName", DefaultValue = "DeviceHive.WebSockets.Host.{0}")]
        public string HostPipeName
        {
            get { return (string) base["hostPipeName"]; }
            set { base["hostPipeName"] = value; }
        }

        /// <summary>
        /// Gets or sets template for application pipe name
        /// </summary>
        [ConfigurationProperty("appPipeName", DefaultValue = "DeviceHive.WebSockets.App.{0}")]
        public string AppPipeName
        {
            get { return (string) base["appPipeName"]; }
            set { base["appPipeName"] = value; }
        }

        /// <summary>
        /// Gets or sets time (in milliseconds) for application terminate
        /// </summary>
        [ConfigurationProperty("appTerminateTimeout", DefaultValue = 1000)]
        public int ApplicationTerminateTimeout
        {
            get { return (int) base["appTerminateTimeout"]; }
            set { base["appTerminateTimeout"] = value; }
        }

        /// <summary>
        /// Gets or sets max time (in minutes) when application can be inactive
        /// </summary>
        [ConfigurationProperty("appInactiveTimeout", DefaultValue = 20)]
        public int ApplicationInactiveTimeout
        {
            get { return (int) base["appInactiveTimeout"]; }
            set { base["appInactiveTimeout"] = value; }
        }

        /// <summary>
        /// Gets or sets штеукмфд (in minutes) to check inactvie applications
        /// </summary>
        [ConfigurationProperty("appInactiveCheckInterval", DefaultValue = 10)]
        public int ApplicationInactiveCheckInterval
        {
            get { return (int)base["appInactiveCheckInterval"]; }
            set { base["appInactiveCheckInterval"] = value; }
        }
    }

    public class RuntimeServiceConfiguration
    {
        private string _configPath;

        public RuntimeServiceConfiguration()
        {
            Applications = new List<ApplicationConfiguration>();
        }

        public List<ApplicationConfiguration> Applications { get; set; }

        public static RuntimeServiceConfiguration Load(string configPath)
        {
            RuntimeServiceConfiguration configuration;

            var xmlSerializer = new XmlSerializer(typeof (RuntimeServiceConfiguration));
            try
            {
                using (var reader = new StreamReader(configPath))
                    configuration = (RuntimeServiceConfiguration) xmlSerializer.Deserialize(reader);
            }
            catch (IOException)
            {
                configuration = new RuntimeServiceConfiguration();
            }

            configuration._configPath = configPath;
            return configuration;
        }

        public void Save()
        {
            var xmlSerializer = new XmlSerializer(typeof (RuntimeServiceConfiguration));
            using (var writer = new StreamWriter(_configPath))
                xmlSerializer.Serialize(writer, this);
        }
    }

    public class ApplicationConfiguration
    {
        public string Host { get; set; }

        public string ExePath { get; set; }

        public string CommandLineArgs { get; set; }
    }
}