using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DeviceHive.Device
{
    /// <summary>
    /// Base class for custom devices.
    /// </summary>
    /// <remarks>
    /// Each specific device should derive from the class and implement the following:
    /// 1. Initialize main device properties in constructor.
    /// 2. Alternatively use <see cref="DeviceAttribute"/>, <see cref="DeviceClassAttribute"/> and <see cref="DeviceEquipmentAttribute"/> attributes.
    /// 3. Override <see cref="Main"/> method and implement desired functionality.
    /// 4. Define command handler methods and decorate them with <see cref="DeviceCommandAttribute"/> attributes.
    /// </remarks>
    public abstract class DeviceBase
    {
        private readonly Dictionary<string, MethodInfo> _deviceCommands;

        #region Public Properties

        /// <summary>
        /// Gets device unique identifier.
        /// The value is initialized from the <see cref="DeviceAttribute"/> attribute set on the current device.
        /// </summary>
        public Guid ID { get; protected set; }

        /// <summary>
        /// Gets device key.
        /// Device key is a private value used for device authentication in DeviceHive.
        /// The value is initialized from the <see cref="DeviceAttribute"/> attribute set on the current device.
        /// </summary>
        public string Key { get; protected set; }

        /// <summary>
        /// Gets device name.
        /// The value is initialized from the <see cref="DeviceAttribute"/> attribute set on the current device.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets initial device status.
        /// Changing the value does not automatically set new value in the DeviceHive server,
        /// the device needs to invoke <see cref="IDeviceServiceChannel.SendStatusUpdate"/> to perform an update.
        /// </summary>
        public string Status { get; protected set; }

        /// <summary>
        /// Gets a dictionary of arbitrary device data used to describe additional properties.
        /// </summary>
        public object Data { get; protected set; }

        /// <summary>
        /// Gets the flag indicating whether device should listen for commands sent to the device.
        /// If true, the device will listen to commands. Otherwise, the device will ignore all incoming commands.
        /// The value is initialized from the <see cref="DeviceAttribute"/> attribute set on the current device.
        /// </summary>
        public bool ListenCommands { get; protected set; }

        /// <summary>
        /// Gets device class name.
        /// The value is initialized from the <see cref="DeviceClassAttribute"/> attribute set on the current device.
        /// </summary>
        public string ClassName { get; protected set; }

        /// <summary>
        /// Gets device class version.
        /// The value is initialized from the <see cref="DeviceClassAttribute"/> attribute set on the current device.
        /// </summary>
        public string ClassVersion { get; protected set; }

        /// <summary>
        /// Gets optional timeout in seconds, after which the DeviceHive framework sets device status to Offline.
        /// The value is initialized from the <see cref="DeviceClassAttribute"/> attribute set on the current device.
        /// </summary>
        public int? ClassOfflineTimeout { get; protected set; }

        /// <summary>
        /// Gets a dictionary of arbitrary device class data used to describe additional properties.
        /// </summary>
        public object ClassData { get; protected set; }

        /// <summary>
        /// Gets the list of device equipment.
        /// The value is initialized from the <see cref="DeviceEquipmentAttribute"/> attributes set on the current device.
        /// </summary>
        public List<DeviceEquipmentInfo> EquipmentInfo { get; protected set; }

        /// <summary>
        /// Gets or sets json serializer used during serialization/deserialization of JToken fields to custom strongly-typed objects.
        /// Default serializer uses CamelCasePropertyNamesContractResolver as a contract resolver.
        /// </summary>
        public JsonSerializer JsonSerializer { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DeviceBase()
        {
            Status = "Online";
            ListenCommands = true;
            JsonSerializer = new JsonSerializer { ContractResolver = new CamelCasePropertyNamesContractResolver() };

            // device fields
            var deviceAttribute = GetType().GetAttributes<DeviceAttribute>(false).FirstOrDefault();
            if (deviceAttribute != null)
            {
                ID = deviceAttribute.ID;
                Key = deviceAttribute.Key;
                Name = deviceAttribute.Name;
                ListenCommands = deviceAttribute.ListenCommands;
            }

            // device class fields
            var deviceClassAttribute = GetType().GetAttributes<DeviceClassAttribute>(false).FirstOrDefault();
            if (deviceClassAttribute != null)
            {
                ClassName = deviceClassAttribute.Name;
                ClassVersion = deviceClassAttribute.Version;
                ClassOfflineTimeout = deviceClassAttribute.OfflineTimeout > 0 ? (int?)deviceClassAttribute.OfflineTimeout : null;
            }

            // equipment fields
            EquipmentInfo = GetType().GetAttributes<DeviceEquipmentAttribute>().Select(e => new DeviceEquipmentInfo(e.Code, e.Name, e.Type)).ToList();

            // initialize command handlers meta-information
            _deviceCommands = new Dictionary<string, MethodInfo>();
            foreach (var method in GetType().PublicGetMethods().Where(p => p.IsDefined(typeof(DeviceCommandAttribute), true)))
            {
                if (method.GetParameters().Length != 2)
                    throw new InvalidOperationException(string.Format("The action method '{0}' must include two parameters: DeviceCommand (or any custom strongly-typed object with ParameterAttribute attributes) and CancellationToken", method.Name));
                if (method.GetParameters()[1].ParameterType != typeof(CancellationToken))
                    throw new InvalidOperationException(string.Format("The second parameter of action method '{0}' must be of CancellationToken type", method.Name));
                if (!typeof(DeviceCommandResult).IsAssignableFrom(method.ReturnType))
                    throw new InvalidOperationException(string.Format("The action method '{0}' must return DeviceCommandResult object", method.Name));
                
                var commandAttribute = method.GetAttributes<DeviceCommandAttribute>().First();
                if (_deviceCommands.ContainsKey(commandAttribute.Name))
                    throw new InvalidOperationException(string.Format("There several action methods with '{0}' command name", commandAttribute.Name));
                _deviceCommands[commandAttribute.Name] = method;
            }
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes device with the <see cref="IDeviceServiceChannel"/> interface.
        /// </summary>
        /// <param name="serviceChannel">Service channel used to communicate with the DeviceHive service.</param>
        public virtual void Initialize(IDeviceServiceChannel serviceChannel)
        {
            if (serviceChannel == null)
                throw new ArgumentNullException("serviceChannel");

            ServiceChannel = serviceChannel;
        }
        
        /// <summary>
        /// Represents main device thread.
        /// </summary>
        /// <param name="token">Thread cancellation token.</param>
        public abstract void Main(CancellationToken token);

        /// <summary>
        /// Handles incoming device command, override to implement custom logic.
        /// Default implementation uses reflection to execute action method with matching <see cref="DeviceCommandAttribute"/> attribute.
        /// </summary>
        /// <param name="command"><see cref="DeviceCommand"/> object to handle.</param>
        /// <param name="token">Thread cancellation token.</param>
        /// <returns><see cref="DeviceCommandResult"/> object with command execution result.</returns>
        public virtual DeviceCommandResult HandleCommand(DeviceCommand command, CancellationToken token)
        {
            MethodInfo method;
            if (_deviceCommands.TryGetValue(command.Name, out method))
            {
                var commandType = method.GetParameters()[0].ParameterType;
                var parameters = commandType == typeof(DeviceCommand) ? command :
                    (command.Parameters == null ? Activator.CreateInstance(commandType) :
                    command.Parameters.ToObject(commandType, JsonSerializer));
                return (DeviceCommandResult)method.Invoke(this, new object[] { parameters, token });
            }

            return OnHandleUnknownCommand(command, token);
        }

        /// <summary>
        /// Executed when device is about to stop.
        /// Default implementation sends Offline status to the server.
        /// </summary>
        public virtual void Stop()
        {
            ServiceChannel.SendStatusUpdate("Offline");
        }
        #endregion

        #region Protected Methods

        /// <summary>
        /// Handles unknown device command, override to implement custom logic.
        /// Default implementation returns <see cref="DeviceCommandResult"/> object with "Failed" result.
        /// </summary>
        /// <param name="command"><see cref="DeviceCommand"/> object to handle.</param>
        /// <param name="token">Thread cancellation token.</param>
        /// <returns><see cref="DeviceCommandResult"/> object</returns>
        protected virtual DeviceCommandResult OnHandleUnknownCommand(DeviceCommand command, CancellationToken token)
        {
            return new DeviceCommandResult("Failed", "There is no handler for this command");
        }
        #endregion

        #region Protected Properties

        /// <summary>
        /// Gets channel to the DeviceHive service.
        /// </summary>
        protected IDeviceServiceChannel ServiceChannel { get; private set; }

        /// <summary>
        /// Gets logger interface.
        /// </summary>
        protected ILog Logger
        {
            get { return LogManager.GetLogger(GetType()); }
        }
        #endregion
    }
}
