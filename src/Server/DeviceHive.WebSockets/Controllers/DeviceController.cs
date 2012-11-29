using System;
using DeviceHive.Core;
using DeviceHive.Core.Mapping;
using DeviceHive.Core.Messaging;
using DeviceHive.Data.Model;
using DeviceHive.WebSockets.Core;
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

	    #endregion

	    #region Constructor

	    public DeviceController(DataContext dataContext, WebSocketServerBase server,
	        JsonMapperManager jsonMapperManager,
	        [Named("DeviceCommand")] SubscriptionManager subscriptionManager,
	        MessageBus messageBus) :
	            base(dataContext, server, jsonMapperManager)
	    {
	        _subscriptionManager = subscriptionManager;
	        _messageBus = messageBus;
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

                case "notification/update":
                    UpdateDeviceNotification();
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
	        throw new NotImplementedException();
	    }

	    private void UpdateDeviceNotification()
	    {
	        throw new NotImplementedException();
	    }

	    private void SubsrcibeToDeviceCommands()
	    {
	        throw new NotImplementedException();
	    }

	    private void UnsubsrcibeFromDeviceCommands()
	    {
	        throw new NotImplementedException();
	    }

	    #endregion

        #region Notification handling

        public void CleanupNotifications(WebSocketConnectionBase connection)
        {
            _subscriptionManager.Cleanup(connection);
        }
        
        #endregion

        #endregion
    }
}