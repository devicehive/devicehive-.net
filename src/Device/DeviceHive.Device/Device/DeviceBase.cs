using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using log4net;

namespace DeviceHive.Device
{
    /// <summary>
    /// Base class for custom devices.
    /// </summary>
    /// <remarks>
    /// Each specific device should derive from the class and implement the following:
    /// 1. Specify <see cref="DeviceAttribute"/> and <see cref="DeviceClassAttribute"/> attributes on the derived class.
    /// 2. Optionally set one or several <see cref="DeviceEquipmentAttribute"/> attributes on the derived class.
    /// 3. Override <see cref="Main"/> method and implement desired functionality.
    /// 4. Define command handler methods and decorate with with <see cref="DeviceCommandAttribute"/> attributes.
    /// </remarks>
    public abstract class DeviceBase
    {
        private readonly DeviceAttribute _device;
        private readonly DeviceClassAttribute _deviceClass;
        private readonly List<DeviceEquipmentInfo> _deviceEquipmentInfo;
        private readonly Dictionary<string, MethodInfo> _deviceCommands;

        #region Public Properties

        /// <summary>
        /// Gets device unique identifier.
        /// The value is set by declating <see cref="DeviceAttribute"/> attribute set on concrete device.
        /// </summary>
        public Guid ID
        {
            get { return _device.ID; }
        }

        /// <summary>
        /// Gets device key.
        /// Device key is a private value used for device authentication in DeviceHive.
        /// The value is received from <see cref="DeviceAttribute"/> attribute set on concrete device.
        /// </summary>
        public string Key
        {
            get { return _device.Key; }
        }

        /// <summary>
        /// Gets device name.
        /// The value is received from <see cref="DeviceAttribute"/> attribute set on concrete device.
        /// </summary>
        public string Name
        {
            get { return _device.Name; }
        }

        /// <summary>
        /// Gets the flag indicating whether device should listen for commands sent to the device.
        /// If true, the device will listen to commands. Otherwise, the device will ignore all incoming commands.
        /// The value is received from <see cref="DeviceAttribute"/> attribute set on concrete device.
        /// </summary>
        public bool ListenCommands
        {
            get { return _device.ListenCommands; }
        }

        /// <summary>
        /// Gets device class name.
        /// The value is received from <see cref="DeviceClassAttribute"/> attribute set on concrete device.
        /// </summary>
        public string ClassName
        {
            get { return _deviceClass.Name; }
        }

        /// <summary>
        /// Gets device class version.
        /// The value is received from <see cref="DeviceClassAttribute"/> attribute set on concrete device.
        /// </summary>
        public string ClassVersion
        {
            get { return _deviceClass.Version; }
        }

        /// <summary>
        /// Gets optional timeout in seconds, after which the DeviceHive framework sets device status to Offline.
        /// The value is received from <see cref="DeviceClassAttribute"/> attribute set on concrete device.
        /// </summary>
        public int? ClassOfflineTimeout
        {
            get { return _deviceClass.OfflineTimeout > 0 ? (int?)_deviceClass.OfflineTimeout : null; }
        }

        /// <summary>
        /// Gets the list of device equipment.
        /// The value is received from <see cref="DeviceEquipmentAttribute"/> attributes set on concrete device.
        /// Override to provide alternative implementation.
        /// </summary>
        public virtual List<DeviceEquipmentInfo> EquipmentInfo
        {
            get { return _deviceEquipmentInfo; }
        }

        /// <summary>
        /// Gets or sets current device status.
        /// Changing the value does not automatically set new value in the DeviceHive server,
        /// the device needs to send "deviceStatus" notification with "status" parameter containing new value.
        /// </summary>
        public virtual string Status { get; protected set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DeviceBase()
        {
            _device = GetType().GetAttributes<DeviceAttribute>(false).FirstOrDefault();
            if (_device == null)
                throw new InvalidOperationException("Device class must declare DeviceAttribute!");

            _deviceClass = GetType().GetAttributes<DeviceClassAttribute>(false).FirstOrDefault();
            if (_deviceClass == null)
                throw new InvalidOperationException("Device class must declare DeviceClassAttribute!");

            _deviceEquipmentInfo = GetType().GetAttributes<DeviceEquipmentAttribute>().Select(e => new DeviceEquipmentInfo(e.Code, e.Name, e.Type)).ToList();

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
        /// Also sets device status to "Online".
        /// </summary>
        /// <param name="serviceChannel">Service channel used to communicate with the DeviceHive service.</param>
        public virtual void Initialize(IDeviceServiceChannel serviceChannel)
        {
            if (serviceChannel == null)
                throw new ArgumentNullException("serviceChannel");

            ServiceChannel = serviceChannel;
            Status = "Online";
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
                var parameters = commandType == typeof(DeviceCommand) ? command : ParameterMapper.Map(command.Parameters, commandType);
                return (DeviceCommandResult)method.Invoke(this, new object[] { parameters, token });
            }

            return OnHandleUnknownCommand(command, token);
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
