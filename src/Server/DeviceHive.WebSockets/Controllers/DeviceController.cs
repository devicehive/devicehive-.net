using System;
using DeviceHive.Core;
using DeviceHive.Core.Business;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.Messaging;
using DeviceHive.Data.Model;
using DeviceHive.WebSockets.Network;
using DeviceHive.WebSockets.Subscriptions;
using Newtonsoft.Json.Linq;
using Ninject;

namespace DeviceHive.WebSockets.Controllers
{
	public class DeviceController : ControllerBase
	{
	    #region Private fields

	    private readonly SubscriptionManager _subscriptionManager;
	    private readonly MessageBus _messageBus;
	    private readonly INotificationManager _notificationManager;

	    #endregion

	    #region Constructor

	    public DeviceController(DataContext dataContext, WebSocketServerBase server,
	        JsonMapperManager jsonMapperManager,
	        [Named("DeviceCommand")] SubscriptionManager subscriptionManager,
	        MessageBus messageBus, INotificationManager notificationManager) :
	            base(dataContext, server, jsonMapperManager)
	    {
	        _subscriptionManager = subscriptionManager;
	        _messageBus = messageBus;
	        _notificationManager = notificationManager;
	    }

	    #endregion

        #region Properties

	    private Device CurrentDevice
	    {
            get { return (Device) Connection.Session["device"]; }
            set { Connection.Session["device"] = value; }
	    }

        #endregion

	    #region Methods

	    #region Actions

	    protected override void InvokeActionImpl()
	    {
	        if (CurrentDevice == null && ActionName != "authenticate")
	            return;

	        switch (ActionName)
	        {
                case "authenticate":
                    Authenticate();
                    break;

                case "notification/insert":
                    InsertDeviceNotification();
                    break;

                case "command/subscribe":
                    SubsrcibeToDeviceCommands();
                    break;

                case "notification/unsubscribe":
                    UnsubsrcibeFromDeviceCommands();
                    break;
	        }
	    }

	    private void Authenticate()
	    {
	        var deviceId = Guid.Parse((string) ActionArgs["deviceId"]);
	        var deviceKey = (string) ActionArgs["deviceKey"];

	        var device = DataContext.Device.Get(deviceId);
            if (device == null || device.Key != deviceKey)
                throw new WebSocketRequestException("Device not found");

	        CurrentDevice = device;
	    }

	    private void InsertDeviceNotification()
	    {
	        var notificationObj = (JObject) ActionArgs["notification"];
	        
            var notification = NotificationMapper.Map(notificationObj);
	        notification.Device = CurrentDevice;
            // todo: validate notification

            DataContext.DeviceNotification.Save(notification);
            _notificationManager.ProcessNotification(notification);
            _messageBus.Notify(new DeviceNotificationAddedMessage(CurrentDevice.GUID, notification.ID));

	        notificationObj = NotificationMapper.Map(notification);
            SendResponse(new JProperty("notification", notificationObj));
	    }

	    private void SubsrcibeToDeviceCommands()
	    {
	        var deviceGuids = ParseDeviceGuids();
	        foreach (var deviceGuid in deviceGuids)
	            _subscriptionManager.Subscribe(Connection, deviceGuid);

            SendSuccessResponse();
	    }

	    private void UnsubsrcibeFromDeviceCommands()
	    {
            var deviceGuids = ParseDeviceGuids();
            foreach (var deviceGuid in deviceGuids)
                _subscriptionManager.Unsubscribe(Connection, deviceGuid);

            SendSuccessResponse();
	    }

	    #endregion

        #region Notification handling

        public void HandleDeviceNotification(Guid deviceGuid, int commandId)
        {
            var command = DataContext.DeviceCommand.Get(commandId);
            var connections = _subscriptionManager.GetConnections(deviceGuid);

            foreach (var connection in connections)
                Notify(connection, command);
        }

        public void CleanupNotifications(WebSocketConnectionBase connection)
        {
            _subscriptionManager.Cleanup(connection);
        }

        private void Notify(WebSocketConnectionBase connection, DeviceCommand command)
        {
            var device = (Device) connection.Session["device"];
            if (device == null || device.ID != command.DeviceID)
                return;

            SendResponse(connection, "command/notify",
                new JProperty("command", CommandMapper.Map(command)));
        }

        #endregion

        #endregion
    }
}