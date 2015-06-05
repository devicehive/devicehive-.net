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
    [AuthorizeCronTrigger]
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
            var devices = DataContext.Device.GetDisconnectedDevices(OFFLINE_STATUS);
            foreach (var device in devices)
            {
                // update device status
                device.Status = OFFLINE_STATUS;
                DataContext.Device.Save(device);

                // save the status diff notification
                var notification = new DeviceNotification(SpecialNotifications.DEVICE_UPDATE, device);
                notification.Parameters = new JObject(new JProperty("status", OFFLINE_STATUS)).ToString();
                DataContext.DeviceNotification.Save(notification);
                _messageBus.Notify(new DeviceNotificationAddedMessage(device.ID, notification.ID, notification.Notification));
            }
        }

        [HttpNoContentResponse]
        [HttpGet, Route("Cleanup")]
        public void Cleanup()
        {
            // cleanup access keys
            DataContext.AccessKey.Cleanup(DateTime.UtcNow);

            // cleanup notifications
            var notificationLifetime = DeviceHiveConfiguration.Maintenance.NotificationLifetime;
            if (notificationLifetime != TimeSpan.Zero)
            {
                var timestamp = DataContext.Timestamp.GetCurrentTimestamp() - notificationLifetime;
                DataContext.DeviceNotification.Cleanup(timestamp);
            }

            // cleanup commands
            var commandLifeTime = DeviceHiveConfiguration.Maintenance.CommandLifetime;
            if (commandLifeTime != TimeSpan.Zero)
            {
                var timestamp = DataContext.Timestamp.GetCurrentTimestamp() - commandLifeTime;
                DataContext.DeviceCommand.Cleanup(timestamp);
            }
        }
    }
}