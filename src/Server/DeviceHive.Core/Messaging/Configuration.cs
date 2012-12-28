using System.Configuration;

namespace DeviceHive.Core.Messaging
{
    /// <summary>
    /// Named pipe message bus configuration section handler
    /// </summary>
    public class NamedPipeMessageBusConfigurationSection : ConfigurationSection
    {
        /// <summary>
        /// Gets named pipes configuration
        /// </summary>
        [ConfigurationProperty("pipes")]
        public NamedPipeElementCollection Pipes
        {
            get { return (NamedPipeElementCollection) base["pipes"]; }
        }

        /// <summary>
        /// Gets or sets timeout for named pipe client connection
        /// </summary>
        [ConfigurationProperty("connectTimeout", DefaultValue = 100)]
        public int ConnectTimeout
        {
            get { return (int) base["connectTimeout"]; }
            set { base["connectTimeout"] = value; }
        }
    }

    /// <summary>
    /// Named pipe element configuration collection
    /// </summary>
    public class NamedPipeElementCollection : ConfigurationElementCollection
    {
        /// <summary>
        /// Creates new element
        /// </summary>
        /// <returns>ConfigurationElement object</returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new NamedPipeElement();
        }

        /// <summary>
        /// Gets element get
        /// </summary>
        /// <param name="element">ConfigurationElement object</param>
        /// <returns>Element key</returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            var pipeElement = (NamedPipeElement) element;
            return string.Format("{0}\\{1}", pipeElement.ServerName, pipeElement.Name);
        }

        /// <summary>
        /// Gets element name
        /// </summary>
        protected override string ElementName
        {
            get { return "pipe"; }
        }

        /// <summary>
        /// Gets collection type
        /// </summary>
        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }
    }

    /// <summary>
    /// Named pipe configuration element
    /// </summary>
    public class NamedPipeElement : ConfigurationElement
    {
        /// <summary>
        /// Gets or sets pipe server name
        /// </summary>
        [ConfigurationProperty("serverName", DefaultValue = ".", IsKey = true)]
        public string ServerName
        {
            get { return (string) this["serverName"]; }
            set { base["serverName"] = value; }
        }

        /// <summary>
        /// Gets or sets pipe name
        /// </summary>
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get { return (string) this["name"]; }
            set { base["name"] = value; }
        }

        /// <summary>
        /// Gets or sets flag indicating that given pipe should be used as server pipe
        /// </summary>
        /// <remarks>
        /// Only one pipe in the collection should be server pipe and all nodes should
        /// use different pipes for server pipe
        /// </remarks>
        [ConfigurationProperty("isServer", DefaultValue = false)]
        public bool IsServer
        {
            get { return (bool) this["isServer"]; }
            set { base["isServer"] = value; }
        }
    }
}