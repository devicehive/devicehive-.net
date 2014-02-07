using System.Configuration;

namespace DeviceHive.Core
{
    /// <summary>
    /// Represents DeviceHive configuration section
    /// </summary>
    public class DeviceHiveConfiguration : ConfigurationSection
    {
        /// <summary>
        /// Gets or sets network configuration element
        /// </summary>
        [ConfigurationProperty("network")]
        public NetworkConfigurationElement Network
        {
            get { return (NetworkConfigurationElement)base["network"] ?? new NetworkConfigurationElement(); }
            set { base["network"] = value; }
        }

        /// <summary>
        /// Gets or sets REST endpoint configuration element
        /// </summary>
        [ConfigurationProperty("restEndpoint")]
        public RestEndpointConfigurationElement RestEndpoint
        {
            get { return (RestEndpointConfigurationElement)base["restEndpoint"] ?? new RestEndpointConfigurationElement(); }
            set { base["restEndpoint"] = value; }
        }

        /// <summary>
        /// Gets or sets WebSocket endpoint configuration element
        /// </summary>
        [ConfigurationProperty("webSocketEndpoint")]
        public WebSocketEndpointConfigurationElement WebSocketEndpoint
        {
            get { return (WebSocketEndpointConfigurationElement)base["webSocketEndpoint"] ?? new WebSocketEndpointConfigurationElement(); }
            set { base["webSocketEndpoint"] = value; }
        }

        /// <summary>
        /// Gets or sets WebSocket endpoint hosting configuration element
        /// </summary>
        [ConfigurationProperty("webSocketEndpointHosting")]
        public WebSocketEndpointHostingConfigurationElement WebSocketEndpointHosting
        {
            get { return (WebSocketEndpointHostingConfigurationElement)base["webSocketEndpointHosting"] ?? new WebSocketEndpointHostingConfigurationElement(); }
            set { base["webSocketEndpointHosting"] = value; }
        }
    }

    /// <summary>
    /// Represents network configuration element
    /// </summary>
    public class NetworkConfigurationElement : ConfigurationElement
    {
        /// <summary>
        /// Gets or sets value indicating if automatic network creation is allowed
        /// </summary>
        [ConfigurationProperty("allowAutoCreate", DefaultValue = false)]
        public bool AllowAutoCreate
        {
            get { return (bool)this["allowAutoCreate"]; }
            set { base["allowAutoCreate"] = value; }
        }
    }

    /// <summary>
    /// Represents REST endpoint configuration element
    /// </summary>
    public class RestEndpointConfigurationElement : ConfigurationElement
    {
        /// <summary>
        /// Gets or sets REST endpoint URL
        /// </summary>
        [ConfigurationProperty("url")]
        public string Uri
        {
            get { return (string)this["url"]; }
            set { base["url"] = value; }
        }
    }

    /// <summary>
    /// Represents WebSocket endpoint configuration element
    /// </summary>
    public class WebSocketEndpointConfigurationElement : ConfigurationElement
    {
        /// <summary>
        /// Gets or sets value indicating if WebSocket endpoint is enabled
        /// </summary>
        [ConfigurationProperty("enabled", DefaultValue = false)]
        public bool Enabled
        {
            get { return (bool)this["enabled"]; }
            set { base["enabled"] = value; }
        }

        /// <summary>
        /// Gets or sets WebSocket endpoint URL
        /// </summary>
        [ConfigurationProperty("url")]
        public string Url
        {
            get { return (string)this["url"]; }
            set { base["url"] = value; }
        }

        /// <summary>
        /// Gets or sets SSL certificate serial number for the WebSocket endpoint
        /// </summary>
        [ConfigurationProperty("sslCertSerialNumber")]
        public string SslCertSerialNumber
        {
            get { return (string)this["sslCertSerialNumber"]; }
            set { base["sslCertSerialNumber"] = value; }
        }
    }

    /// <summary>
    /// Represents WebSocket endpoint hosting configuration element
    /// </summary>
    public class WebSocketEndpointHostingConfigurationElement : ConfigurationElement
    {
        /// <summary>
        /// Gets or sets pipe name of the host service
        /// </summary>
        [ConfigurationProperty("hostPipeName")]
        public string HostPipeName
        {
            get { return (string)this["hostPipeName"]; }
            set { base["hostPipeName"] = value; }
        }

        /// <summary>
        /// Gets or sets pipe name of the current application service
        /// </summary>
        [ConfigurationProperty("appPipeName")]
        public string AppPipeName
        {
            get { return (string)this["appPipeName"]; }
            set { base["appPipeName"] = value; }
        }
    }
}
