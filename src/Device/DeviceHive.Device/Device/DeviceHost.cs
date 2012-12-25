using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json.Linq;

namespace DeviceHive.Device
{
    /// <summary>
    /// Represents device host implementation.
    /// </summary>
    /// <remarks>
    /// The class is capable of hosting one or several devices, which could be added via <see cref="AddDevice"/> method.
    /// When started, the host runs all devices and performs the following two actions:
    /// 1. Routes incomings DeviceHive commands to the corresponding devices.
    /// 2. Allows devices to dispatch notifications using <see cref="SendNotification"/> method.
    /// </remarks>
    public class DeviceHost
    {
        private readonly List<DeviceBase> _devices = new List<DeviceBase>();

        private List<Task> _tasks;
        private CancellationTokenSource _cancellationSource;

        #region Public Properties

        /// <summary>
        /// Gets read-only collection of devices associated with the current host.
        /// </summary>
        public IList<DeviceBase> Devices
        {
            get { return _devices.AsReadOnly(); }
        }

        /// <summary>
        /// Gets associated DeviceHive device client.
        /// </summary>
        public IDeviceService DeviceClient { get; private set; }

        /// <summary>
        /// Gets associated DeviceHive network object.
        /// If specified network is not found in the DeviceHive service, it will be automatically created.
        /// </summary>
        public Network Network { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="client">Associated DeviceHive device client. Use any custom or default <see cref="RestfulDeviceService"/> implementation.</param>
        /// <param name="network">Associated DeviceHive network object (optional). If specified network is not found in the DeviceHive service, it will be automatically created.</param>
        public DeviceHost(IDeviceService client, Network network)
        {
            if (client == null)
                throw new ArgumentNullException("client");

            DeviceClient = client;
            Network = network;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Adds new device to the host.
        /// </summary>
        /// <param name="device"><see cref="DeviceBase"/> object to add.</param>
        public void AddDevice(DeviceBase device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            device.Initialize(new DeviceServiceChannel(this, device));
            _devices.Add(device);
        }

        /// <summary>
        /// Start all hosted devices by launching their <see cref="DeviceBase.Main"/> methods and routing incoming DeviceHive commands.
        /// </summary>
        public void Start()
        {
            if (_tasks != null)
                throw new InvalidOperationException("The host is already running!");

            Logger.Info("Starting device host");

            // register all devices
            foreach (var device in Devices)
            {
                RegisterDevice(device);
            }

            // start device's main and command polling tasks
            _tasks = new List<Task>();
            _cancellationSource = new CancellationTokenSource();
            var token = _cancellationSource.Token;

            DeviceClient.CommandInserted += (s, e) =>
            {
                var device = Devices.FirstOrDefault(d => d.ID == e.DeviceGuid);
                if (device != null)
                    Task.Factory.StartNew(() => DispatchCommandTask(device, e.Command));
            };

            DeviceClient.ConnectionClosed += (s, e) => SubscribeToCommands();

            foreach (var device in Devices)
            {
                Logger.InfoFormat("Staring device {0} ({1})", device.ID, device.Name);
                var deviceCopy = device;
                _tasks.Add(Task.Factory.StartNew(() => MainDeviceTask(deviceCopy), token, TaskCreationOptions.LongRunning, TaskScheduler.Default));
            }

            SubscribeToCommands();

            Logger.Info("Device host is now running");
        }

        /// <summary>
        /// Stops all hosted devices by propagating cancellation event to all threads.
        /// The method waits until all threads are actually stopped before exiting.
        /// </summary>
        public void Stop()
        {
            if (_tasks == null)
                throw new InvalidOperationException("The host is not currenly running!");

            Logger.Info("Stopping device host");

            // stop all devices
            foreach (var device in Devices)
            {
                device.Stop();
            }

            // cancel all tasks
            _cancellationSource.Cancel();
            try
            {
                Task.WaitAll(_tasks.ToArray());
            }
            catch (AggregateException)
            {
            }

            // unsubscribe from commands
            foreach (var device in Devices)
            {
                DeviceClient.UnsubscribeFromCommands(device.ID, device.Key);
            }
            
            // reset the properties
            _tasks = null;
            _cancellationSource = null;

            Logger.Info("Device host is now stopped");
        }

        /// <summary>
        /// Sends a device status update.
        /// </summary>
        /// <param name="sender">Sender <see cref="DeviceBase"/> object.</param>
        /// <param name="status">New device status.</param>
        public void SendStatusUpdate(DeviceBase sender, string status)
        {
            if (sender == null)
                throw new ArgumentNullException("sender");
            if (string.IsNullOrEmpty(status))
                throw new ArgumentException("Status is null or empty!", "status");

            Logger.InfoFormat("Updating device {1} ({2}) status to '{0}'", status, sender.ID, sender.Name);

            try
            {
                var cDevice = new Device(sender.ID, sender.Key) { Status = status };
                DeviceClient.UpdateDevice(cDevice);
            }
            catch (Exception ex)
            {
                // critical error - log and fault the service
                Logger.Error(string.Format("Exception while updating device {1} ({2}) status to '{0}'", status, sender.ID, sender.Name), ex);
                throw;
            }
        }

        /// <summary>
        /// Sends a notification on behalf of the specified device.
        /// </summary>
        /// <param name="sender">Sender <see cref="DeviceBase"/> object.</param>
        /// <param name="notification"><see cref="DeviceNotification"/> object to send.</param>
        public void SendNotification(DeviceBase sender, DeviceNotification notification)
        {
            if (sender == null)
                throw new ArgumentNullException("sender");
            if (notification == null)
                throw new ArgumentNullException("notification");

            Logger.InfoFormat("Sending notification '{0}' from device {1} ({2})", notification.Name, sender.ID, sender.Name);

            try
            {
                var cNotification = new Notification(notification.Name.Trim(),
                    notification.Parameters == null ? null : notification.Parameters.DeepClone());
                DeviceClient.SendNotification(sender.ID, sender.Key, cNotification);
            }
            catch (Exception ex)
            {
                // critical error - log and fault the service
                Logger.Error(string.Format("Exception while sending notification '{0}' from device {1} ({2})", notification.Name, sender.ID, sender.Name), ex);
                throw;
            }
        }
        #endregion

        #region Private Methods

        private void SubscribeToCommands()
        {
            foreach (var device in Devices)
                SubscribeToCommands(device);
        }

        private void SubscribeToCommands(DeviceBase device)
        {
            Logger.InfoFormat("Subscribe to device {0} ({1}) commands", device.ID, device.Name);
            if (device.ListenCommands)
            {
                while (true)
                {
                    try
                    {
                        DeviceClient.SubscribeToCommands(device.ID, device.Key);
                        break;
                    }
                    catch (DeviceServiceException e)
                    {
                        Logger.ErrorFormat("Error when subscribing to device {0} ({1}) commands: {2}",
                            device.ID, device.Name, e);

                        // retry with small wait
                        Thread.Sleep(100);
                    }
                }
            }
        }
        
        private void RegisterDevice(DeviceBase device)
        {
            Logger.InfoFormat("Registering device {0} ({1})", device.ID, device.Name);

            try
            {
                var cDeviceClass = new DeviceClass(device.ClassName, device.ClassVersion, device.ClassOfflineTimeout,
                    device.ClassData == null ? null : JToken.FromObject(device.ClassData, device.JsonSerializer));
                var cDevice = new Device(device.ID, device.Key, device.Name, device.Status,
                    device.Data == null ? null : JToken.FromObject(device.Data, device.JsonSerializer), Network, cDeviceClass);
                cDevice.Equipment = device.EquipmentInfo.Select(e => new Equipment(
                    e.Name, e.Code, e.Type, e.Data == null ? null : JToken.FromObject(e.Data, device.JsonSerializer))).ToList();
                DeviceClient.RegisterDevice(cDevice);
            }
            catch (Exception ex)
            {
                // critical error - log and fault the service
                Logger.Error(string.Format("Exception while registering device {0} ({1}), rethrowing exception", device.ID, device.Name), ex);
                throw;
            }
        }

        private void MainDeviceTask(DeviceBase device)
        {
            var token = _cancellationSource.Token;
            try
            {
                device.Main(token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                // operation faulted - log the error and stop the task
                Logger.Error(string.Format("Exception in main thread of device {0} ({1})", device.ID, device.Name), ex);
                throw;
            }
        }

        private void DispatchCommandTask(DeviceBase device, Command cCommand)
        {
            // invoke device
            DeviceCommandResult result;
            try
            {
                var command = new DeviceCommand(cCommand.Name.Trim(),
                    cCommand.Parameters == null ? null : cCommand.Parameters.DeepClone());
                result = device.HandleCommand(command, _cancellationSource.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                // operation faulted - log the error and send failed result
                Logger.Error(string.Format("Exception while handling a command '{0}' by device {1} ({2})", cCommand.Name, device.ID, device.Name), ex);
                result = new DeviceCommandResult("Failed", "An error occurred while handling the command");
            }
                    
            // send command result
            cCommand.Status = result.Status;
            cCommand.Result = result.Result == null ? null : JToken.FromObject(result.Result, device.JsonSerializer);
            SendCommandResult(device, cCommand);
        }

        private void SendCommandResult(DeviceBase device, Command cCommand)
        {
            Logger.InfoFormat("Sending command '{0}' status '{1}' from device {2} ({3})",
                cCommand.Name, cCommand.Status, device.ID, device.Name);

            try
            {
                DeviceClient.UpdateCommand(device.ID, device.Key, cCommand);
            }
            catch (Exception ex)
            {
                // not critical - do nothing
                Logger.Error(string.Format("Exception while sending command '{0}' status '{1}' from device {2} ({3})",
                    cCommand.Name, cCommand.Status, device.ID, device.Name), ex);
            }
        }
        #endregion

        #region Private Properties

        private ILog Logger
        {
            get { return LogManager.GetLogger(GetType()); }
        }
        #endregion

        #region DeviceServiceChannel class

        private class DeviceServiceChannel : IDeviceServiceChannel
        {
            private readonly DeviceHost _host;
            private readonly DeviceBase _device;

            #region Constructor

            public DeviceServiceChannel(DeviceHost host, DeviceBase device)
            {
                _host = host;
                _device = device;
            }
            #endregion

            #region IDeviceServiceChannel Members

            public void SendStatusUpdate(string status)
            {
                _host.SendStatusUpdate(_device, status);
            }

            public void SendNotification(DeviceNotification notification)
            {
                _host.SendNotification(_device, notification);
            }

            public void SendNotification(string notification, object parameters)
            {
                SendNotification(new DeviceNotification(notification,
                    parameters == null ? null : JToken.FromObject(parameters, _device.JsonSerializer)));
            }

            public void SendEquipmentNotification(string equipment, object parameters)
            {
                if (string.IsNullOrEmpty(equipment))
                    throw new ArgumentException("Equipment is null or empty!", "equipment");

                var notificationParameters = parameters == null ?
                    new JObject() : JObject.FromObject(parameters, _device.JsonSerializer);
                notificationParameters["equipment"] = equipment;
                SendNotification(new DeviceNotification("equipment", notificationParameters));
            }
            #endregion
        }
        #endregion
    }
}
