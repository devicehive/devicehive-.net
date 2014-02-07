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

        /// <summary>
        /// Gets or sets user password policy configuration element
        /// </summary>
        [ConfigurationProperty("userPasswordPolicy")]
        public UserPasswordPolicyConfigurationElement UserPasswordPolicy
        {
            get { return (UserPasswordPolicyConfigurationElement)base["userPasswordPolicy"] ?? new UserPasswordPolicyConfigurationElement(); }
            set { base["userPasswordPolicy"] = value; }
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

    /// <summary>
    /// Represents user password policy configuration element
    /// </summary>
    public class UserPasswordPolicyConfigurationElement : ConfigurationElement
    {
        /// <summary>
        /// Gets or sets required password complexity level.
        /// Available values:
        /// <list type="bullet">
        ///     <item><description>0: No password complexity check is performed</description></item>
        ///     <item><description>1: The password must contain both letters and numbers</description></item>
        ///     <item><description>2: The password must contain lower and upper letters and numbers</description></item>
        ///     <item><description>3: The password must contain lower and upper letters, numbers and special characters</description></item>
        /// </list>
        /// Default value is 1 (both letters and numbers).
        /// </summary>
        [IntegerValidator(MinValue = 0, MaxValue = 3)]
        [ConfigurationProperty("complexityLevel", DefaultValue = 1)]
        public int ComplexityLevel
        {
            get { return (int)this["complexityLevel"]; }
            set { base["complexityLevel"] = value; }
        }

        /// <summary>
        /// Gets or sets minumum password length.
        /// Default value is 8.
        /// </summary>
        [IntegerValidator(MinValue = 0)]
        [ConfigurationProperty("minLength", DefaultValue = 8)]
        public int MinLength
        {
            get { return (int)this["minLength"]; }
            set { base["minLength"] = value; }
        }

        /// <summary>
        /// Gets or sets maximum number of invalid login attempts before the user account is locked out.
        /// Set 0 to disable lockout mechanism.
        /// Default value is 10.
        /// </summary>
        [IntegerValidator(MinValue = 0)]
        [ConfigurationProperty("maxLoginAttempts", DefaultValue = 10)]
        public int MaxLoginAttempts
        {
            get { return (int)this["maxLoginAttempts"]; }
            set { base["maxLoginAttempts"] = value; }
        }
    }
}
