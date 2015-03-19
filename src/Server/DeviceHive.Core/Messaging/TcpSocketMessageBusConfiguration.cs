using System.Configuration;

namespace DeviceHive.Core.Messaging
{
    /// <summary>
    /// Tcp socket message bus configuration section handler
    /// </summary>
    public class TcpSocketMessageBusConfiguration : ConfigurationSection
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public TcpSocketMessageBusConfiguration()
        {
            ConnectTimeout = 100;
        }

        /// <summary>
        /// Gets or sets server endpoint
        /// </summary>
        [ConfigurationProperty("serverPort", IsRequired = true)]
        public int ServerPort
        {
            get { return (int)base["serverPort"]; }
            set { base["serverPort"] = value; }
        }

        /// <summary>
        /// Gets client endpoints configuration
        /// </summary>
        [ConfigurationProperty("clientEndpoints")]
        public TcpSocketEndpointsElementCollection ClientEndpoints
        {
            get { return (TcpSocketEndpointsElementCollection)base["clientEndpoints"]; }
        }

        /// <summary>
        /// Gets or sets timeout for tcp socket client connection
        /// </summary>
        [ConfigurationProperty("connectTimeout", DefaultValue = 100)]
        public int ConnectTimeout
        {
            get { return (int)base["connectTimeout"]; }
            set { base["connectTimeout"] = value; }
        }
    }

    /// <summary>
    /// Tcp socket endpoints configuration element collection
    /// </summary>
    public class TcpSocketEndpointsElementCollection : ConfigurationElementCollection
    {
        /// <summary>
        /// Creates new element
        /// </summary>
        /// <returns>ConfigurationElement object</returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new TcpSocketEndpointElement();
        }

        /// <summary>
        /// Gets element get
        /// </summary>
        /// <param name="element">ConfigurationElement object</param>
        /// <returns>Element key</returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            var endpointElement = (TcpSocketEndpointElement)element;
            return string.Format("{0}:{1}", endpointElement.Host, endpointElement.Port);
        }
    }

    /// <summary>
    /// Tcp socket endpoint configuration element
    /// </summary>
    public class TcpSocketEndpointElement : ConfigurationElement
    {
        /// <summary>
        /// Gets or sets endpoint host
        /// </summary>
        [ConfigurationProperty("host", DefaultValue = "127.0.0.1", IsKey = true)]
        public string Host
        {
            get { return (string)this["host"]; }
            set { base["host"] = value; }
        }

        /// <summary>
        /// Gets or sets endpoint port
        /// </summary>
        [ConfigurationProperty("port", IsRequired = true, IsKey = true)]
        public int Port
        {
            get { return (int)this["port"]; }
            set { base["port"] = value; }
        }
    }
}