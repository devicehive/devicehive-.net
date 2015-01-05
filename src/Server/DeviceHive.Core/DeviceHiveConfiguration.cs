using System;
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

        /// <summary>
        /// Gets or sets authentication configuration element
        /// </summary>
        [ConfigurationProperty("authentication")]
        public AuthenticationConfigurationElement Authentication
        {
            get { return (AuthenticationConfigurationElement)base["authentication"] ?? new AuthenticationConfigurationElement(); }
            set { base["authentication"] = value; }
        }

        /// <summary>
        /// Gets or sets message handlers configuration element
        /// </summary>
        [ConfigurationProperty("messageHandlers")]
        public MessageHandlersConfigurationElement MessageHandlers
        {
            get { return (MessageHandlersConfigurationElement)base["messageHandlers"] ?? new MessageHandlersConfigurationElement(); }
            set { base["messageHandlers"] = value; }
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

        /// <summary>
        /// Gets or sets default notification polling interval
        /// </summary>
        [ConfigurationProperty("notificationPollDefaultInterval", DefaultValue = 30)]
        public int NotificationPollDefaultInterval
        {
            get { return (int)this["notificationPollDefaultInterval"]; }
            set { base["notificationPollDefaultInterval"] = value; }
        }

        /// <summary>
        /// Gets or sets maximum notification polling interval
        /// </summary>
        [ConfigurationProperty("notificationPollMaxInterval", DefaultValue = 60)]
        public int NotificationPollMaxInterval
        {
            get { return (int)this["notificationPollMaxInterval"]; }
            set { base["notificationPollMaxInterval"] = value; }
        }

        /// <summary>
        /// Gets or sets default command polling interval
        /// </summary>
        [ConfigurationProperty("commandPollDefaultInterval", DefaultValue = 30)]
        public int CommandPollDefaultInterval
        {
            get { return (int)this["commandPollDefaultInterval"]; }
            set { base["commandPollDefaultInterval"] = value; }
        }

        /// <summary>
        /// Gets or sets maximum command polling interval
        /// </summary>
        [ConfigurationProperty("commandPollMaxInterval", DefaultValue = 60)]
        public int CommandPollMaxInterval
        {
            get { return (int)this["commandPollMaxInterval"]; }
            set { base["commandPollMaxInterval"] = value; }
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
    }

    /// <summary>
    /// Represents authentication configuration element
    /// </summary>
    public class AuthenticationConfigurationElement : ConfigurationElement
    {
        /// <summary>
        /// Gets or sets timeout of the session after user authentication.
        /// Default value is one hour.
        /// </summary>
        [ConfigurationProperty("sessionTimeout", DefaultValue = "00:20:00")]
        public TimeSpan SessionTimeout
        {
            get { return (TimeSpan)this["sessionTimeout"]; }
            set { base["sessionTimeout"] = value; }
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

        /// <summary>
        /// Gets or sets name of the password authentication provider.
        /// </summary>
        [ConfigurationProperty("passwordProviderName", DefaultValue = "password")]
        public string PasswordProviderName
        {
            get { return (string)this["passwordProviderName"]; }
            set { base["passwordProviderName"] = value; }
        }

        /// <summary>
        /// Gets or sets redirect uri to pass during OAuth authentication code exchange.
        /// </summary>
        [ConfigurationProperty("oauthRedirectUri")]
        public string OAuthRedirectUri
        {
            get { return (string)this["oauthRedirectUri"]; }
            set { base["oauthRedirectUri"] = value; }
        }

        /// <summary>
        /// Gets or sets authentication providers configuration element
        /// </summary>
        [ConfigurationProperty("providers")]
        public AuthenticationProvidersConfigurationElement Providers
        {
            get { return (AuthenticationProvidersConfigurationElement)base["providers"] ?? new AuthenticationProvidersConfigurationElement(); }
            set { base["providers"] = value; }
        }
    }

    /// <summary>
    /// Represents authentication providers configuration element
    /// </summary>
    public class AuthenticationProvidersConfigurationElement : ConfigurationElementCollection
    {
        /// <summary>
        /// Gets collection type.
        /// </summary>
        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.AddRemoveClearMap; }
        }

        /// <summary>
        /// Creates new child element.
        /// </summary>
        /// <returns>MessageHandlerConfigurationElement object.</returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new AuthenticationProviderConfigurationElement();
        }

        /// <summary>
        /// Gets element key.
        /// </summary>
        /// <param name="element">MessageHandlerConfigurationElement object.</param>
        /// <returns>Element key.</returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as AuthenticationProviderConfigurationElement).Type;
        }
    }

        /// <summary>
    /// Represents message handlers configuration element
    /// </summary>
    public class AuthenticationProviderConfigurationElement : ConfigurationElement
    {
        /// <summary>
        /// Gets or sets name of the authentication provider.
        /// </summary>
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { base["name"] = value; }
        }

        /// <summary>
        /// Gets or sets type of the authentication provider.
        /// </summary>
        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get { return (string)this["type"]; }
            set { base["type"] = value; }
        }

        /// <summary>
        /// Gets or sets value indicating if authentication provider is enabled
        /// </summary>
        [ConfigurationProperty("enabled", DefaultValue = true)]
        public bool Enabled
        {
            get { return (bool)this["enabled"]; }
            set { base["enabled"] = value; }
        }

        /// <summary>
        /// Gets or sets client identifier of third-party identity provider.
        /// </summary>
        [ConfigurationProperty("clientId")]
        public string ClientId
        {
            get { return (string)this["clientId"]; }
            set { base["clientId"] = value; }
        }

        /// <summary>
        /// Gets or sets client secret of third-party identity provider.
        /// </summary>
        [ConfigurationProperty("clientSecret")]
        public string ClientSecret
        {
            get { return (string)this["clientSecret"]; }
            set { base["clientSecret"] = value; }
        }

        /// <summary>
        /// Gets or sets a custom argument to pass into the provider constructor.
        /// </summary>
        [ConfigurationProperty("argument")]
        public string Argument
        {
            get { return (string)this["argument"]; }
            set { base["argument"] = value; }
        }
    }

    /// <summary>
    /// Represents message handlers configuration element
    /// </summary>
    public class MessageHandlersConfigurationElement : ConfigurationElementCollection
    {
        /// <summary>
        /// Gets collection type.
        /// </summary>
        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.AddRemoveClearMap; }
        }

        /// <summary>
        /// Creates new child element.
        /// </summary>
        /// <returns>MessageHandlerConfigurationElement object.</returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new MessageHandlerConfigurationElement();
        }

        /// <summary>
        /// Gets element key.
        /// </summary>
        /// <param name="element">MessageHandlerConfigurationElement object.</param>
        /// <returns>Element key.</returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as MessageHandlerConfigurationElement).Type;
        }
    }

    /// <summary>
    /// Represents message handlers configuration element
    /// </summary>
    public class MessageHandlerConfigurationElement : ConfigurationElement
    {
        /// <summary>
        /// Gets or sets type of the message handler.
        /// </summary>
        [ConfigurationProperty("type", IsKey = true, IsRequired = true)]
        public string Type
        {
            get { return (string)this["type"]; }
            set { base["type"] = value; }
        }

        /// <summary>
        /// Gets or sets a custom argument to pass into the handler constructor.
        /// </summary>
        [ConfigurationProperty("argument")]
        public string Argument
        {
            get { return (string)this["argument"]; }
            set { base["argument"] = value; }
        }

        /// <summary>
        /// Gets or sets a comma-separated list of notification names to handle.
        /// </summary>
        [ConfigurationProperty("notificationNames")]
        public string NotificationNames
        {
            get { return (string)this["notificationNames"]; }
            set { base["notificationNames"] = value; }
        }

        /// <summary>
        /// Gets or sets a comma-separated list of command names to handle.
        /// </summary>
        [ConfigurationProperty("commandNames")]
        public string CommandNames
        {
            get { return (string)this["commandNames"]; }
            set { base["commandNames"] = value; }
        }

        /// <summary>
        /// Gets or sets a comma-separated list of device guids to handle.
        /// </summary>
        [ConfigurationProperty("deviceGuids")]
        public string DeviceGuids
        {
            get { return (string)this["deviceGuids"]; }
            set { base["deviceGuids"] = value; }
        }

        /// <summary>
        /// Gets or sets a comma-separated list of device class ids to handle.
        /// </summary>
        [ConfigurationProperty("deviceClassIds")]
        public string DeviceClassIds
        {
            get { return (string)this["deviceClassIds"]; }
            set { base["deviceClassIds"] = value; }
        }

        /// <summary>
        /// Gets or sets a comma-separated list of network ids to handle.
        /// </summary>
        [ConfigurationProperty("networkIds")]
        public string NetworkIds
        {
            get { return (string)this["networkIds"]; }
            set { base["networkIds"] = value; }
        }
    }
}
