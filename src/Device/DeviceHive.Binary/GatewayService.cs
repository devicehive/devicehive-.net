using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DeviceHive.Device;
using log4net;

namespace DeviceHive.Binary
{
    /// <summary>
    /// DeviceHive gateway service implementation
    /// </summary>
    public class GatewayService : IDisposable
    {
        private readonly object _lock = new object();

        private readonly IDeviceService _deviceService;
        private readonly IList<IBinaryConnection> _deviceConnectionList;

        private readonly ILog _logger;

        private readonly IDictionary<IBinaryConnection, DeviceGatewayService> _deviceGatewayServicesByConnection =
            new Dictionary<IBinaryConnection, DeviceGatewayService>();

        private readonly IDictionary<Guid, DeviceGatewayService> _deviceGatewayServicesByDevice =
            new Dictionary<Guid, DeviceGatewayService>();


        /// <summary>
        /// Initialize instance of <see cref="GatewayService"/>
        /// </summary>
        /// <param name="deviceService">Service for interaction with DeviceHive server</param>
        /// <param name="network">DeviceHive network</param>
        public GatewayService(IDeviceService deviceService, Network network)
        {            
            _deviceService = deviceService;
            _deviceConnectionList = new BinaryConnectionList(OnAddConnection, OnRemoveConnection);
            _logger = LogManager.GetLogger(GetType());

            Network = network;            
        }

        
        /// <summary>
        /// Gets list of device connections
        /// </summary>
        public IList<IBinaryConnection> DeviceConnectionList
        {
            get { return _deviceConnectionList; }
        }

        /// <summary>
        /// Gets DeviceHive network
        /// </summary>
        public Network Network { get; private set; }


        /// <summary>
        /// Gets flag indicating that gateway is started
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Starts gateway
        /// </summary>
        public void Start()
        {
            if (IsStarted)
                return;

            lock (_lock)
            {
                if (IsStarted)
                    return;

                _deviceService.CommandInserted += OnCommandInserted;
                _deviceService.ConnectionClosed += OnConnectionClosed;

                foreach (var deviceGatewayService in _deviceGatewayServicesByConnection.Values)
                    deviceGatewayService.Start();

                IsStarted = true;
            }
        }

        /// <summary>
        /// Stops gateway
        /// </summary>
        public void Stop()
        {
            if (!IsStarted)
                return;

            lock (_lock)
            {
                if (!IsStarted)
                    return;

                _deviceService.CommandInserted -= OnCommandInserted;
                _deviceService.ConnectionClosed -= OnConnectionClosed;

                foreach (var deviceGatewayService in _deviceGatewayServicesByConnection.Values)
                    deviceGatewayService.Stop();
            }
        }


        private void OnAddConnection(IBinaryConnection binaryConnection)
        {
            var deviceGatewayService = new DeviceGatewayService(binaryConnection, this);
            _deviceGatewayServicesByConnection.Add(binaryConnection, deviceGatewayService);

            if (IsStarted)
                deviceGatewayService.Start();
        }

        private void OnRemoveConnection(IBinaryConnection binaryConnection)
        {
            DeviceGatewayService deviceGatewayService;            
            if (!_deviceGatewayServicesByConnection.TryGetValue(binaryConnection, out deviceGatewayService))
            {
                binaryConnection.Dispose();
            }
            else
            {
                var deviceGuid = deviceGatewayService.DeviceGuid;
                if (deviceGuid != Guid.Empty)
                {
                    _deviceService.UnsubscribeFromCommands(deviceGuid, deviceGatewayService.DeviceKey);
                    _deviceGatewayServicesByDevice.Remove(deviceGuid);
                }

                deviceGatewayService.Stop();
            }

            _deviceGatewayServicesByConnection.Remove(binaryConnection);
        }

        private bool CheckDevice(Guid deviceGuid)
        {
            if (!_deviceGatewayServicesByDevice.ContainsKey(deviceGuid))
                return true;

            _logger.ErrorFormat("Device with GUID = {0} already connected. Skip.", deviceGuid);
            return false;
        }

        private void RegisterDevice(DeviceGatewayService deviceGatewayService)
        {
            var deviceGuid = deviceGatewayService.DeviceGuid;
            _deviceGatewayServicesByDevice.Add(deviceGuid, deviceGatewayService);
            _deviceService.SubscribeToCommands(deviceGuid, deviceGatewayService.DeviceKey);
        }

        private void OnCommandInserted(object sender, CommandEventArgs args)
        {
            DeviceGatewayService deviceGatewayService;
            if (!_deviceGatewayServicesByDevice.TryGetValue(args.DeviceGuid, out deviceGatewayService))
                return;

            deviceGatewayService.HandleCommand(args.Command);
        }

        private void OnConnectionClosed(object sender, EventArgs eventArgs)
        {
            foreach (var deviceGatewayService in _deviceGatewayServicesByDevice.Values)
            {
                _deviceService.SubscribeToCommands(
                    deviceGatewayService.DeviceGuid,
                    deviceGatewayService.DeviceKey);
            }
        }


        #region Inner classes

        private class DeviceGatewayService : BinaryServiceBase
        {
            private readonly GatewayService _gatewayService;

            private Guid _deviceGuid;
            private string _deviceKey;

            public DeviceGatewayService(IBinaryConnection connection, GatewayService gatewayService) :
                base(connection)
            {
                _gatewayService = gatewayService;
            }

            public Guid DeviceGuid
            {
                get { return _deviceGuid; }
            }

            public string DeviceKey
            {
                get { return _deviceKey; }
            }

            public override void Start()
            {
                base.Start();
                RequestRegistration();
            }

            public void HandleCommand(Command command)
            {
                try
                {
                    SendCommand(command);
                }
                catch (Exception e)
                {
                    _gatewayService._logger.Error(
                        string.Format("Can't handle command: {0}", command.Id), e);
                }
            }

            protected override void RegisterDevice(DeviceRegistrationInfo registrationInfo)
            {
                _deviceGuid = registrationInfo.Id;
                _deviceKey = registrationInfo.Key;

                if (!_gatewayService.CheckDevice(_deviceGuid))
                {
                    // reissue device registration request
                    RequestRegistration();
                    return;
                }

                var device = new Device.Device(_deviceGuid, _deviceKey);
                device.Name = registrationInfo.Name;
                device.DeviceClass = new DeviceClass(registrationInfo.ClassName, registrationInfo.ClassVersion);
                device.Equipment = registrationInfo.Equipment
                    .Select(e => new Equipment(e.Name, e.Code, e.TypeName)).ToList();
                device.Network = _gatewayService.Network;

                DeviceService.RegisterDevice(device);
                _gatewayService.RegisterDevice(this);
            }

            protected override void NotifyCommandResult(int commandId, string status, string result)
            {
                DeviceService.UpdateCommand(_deviceGuid, _deviceKey,
                    new Command() { Id = commandId, Status = status, Result = result });
            }

            protected override void HandleHotification(Notification notification)
            {
                DeviceService.SendNotification(_deviceGuid, _deviceKey, notification);
            }

            private IDeviceService DeviceService
            {
                get { return _gatewayService._deviceService; }
            }
        }

        #endregion


        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Stop();
        }

        #endregion
    }

    internal class BinaryConnectionList : Collection<IBinaryConnection>
    {
        private readonly Action<IBinaryConnection> _onAdd;
        private readonly Action<IBinaryConnection> _onRemove;

        public BinaryConnectionList(Action<IBinaryConnection> onAdd, Action<IBinaryConnection> onRemove)
        {
            _onAdd = onAdd;
            _onRemove = onRemove;
        }

        protected override void ClearItems()
        {
            foreach (var binaryConnection in Items)
                _onRemove(binaryConnection);

            base.ClearItems();
        }

        protected override void InsertItem(int index, IBinaryConnection item)
        {
            if (Contains(item))
                throw new InvalidOperationException("Duplicate binary connection added");

            _onAdd(item);

            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            var item = this[index];
            _onRemove(item);

            base.RemoveItem(index);
        }

        protected override void SetItem(int index, IBinaryConnection item)
        {
            if (Contains(item))
                throw new InvalidOperationException("Duplicate binary connection added");

            var oldItem = this[index];
            _onRemove(oldItem);
            _onAdd(item);

            base.SetItem(index, item);
        }
    }
}