using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using DeviceHive.API.Filters;
using DeviceHive.Core.MessageLogic;
using DeviceHive.Core.Messaging;
using DeviceHive.Data.Model;
using Newtonsoft.Json.Linq;

namespace DeviceHive.API.Controllers
{
    [RoutePrefix("cron")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class CronController : BaseController
    {
        private const string OFFLINE_STATUS = "Offline";

        private readonly MessageBus _messageBus;

        public CronController(MessageBus messageBus)
        {
            _messageBus = messageBus;
        }

        [HttpNoContentResponse]
        [HttpGet, Route("RefreshDeviceStatus")]
        public void RefreshDeviceStatus()
        {
            var devices = DataContext.Device.GetOfflineDevices();
            foreach (var device in devices.Where(d => d.Status != OFFLINE_STATUS))
            {
                // update device status
                device.Status = OFFLINE_STATUS;
                DataContext.Device.Save(device);

                // save the status diff notification
                var notification = new DeviceNotification(SpecialNotifications.DEVICE_UPDATE, device);
                notification.Parameters = new JObject(new JProperty("status", OFFLINE_STATUS)).ToString();
                DataContext.DeviceNotification.Save(notification);
                _messageBus.Notify(new DeviceNotificationAddedMessage(device.ID, notification.ID));
            }
        }
    }
}