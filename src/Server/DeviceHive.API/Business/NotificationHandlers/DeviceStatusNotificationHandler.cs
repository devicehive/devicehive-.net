using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;

namespace DeviceHive.API.Business.NotificationHandlers
{
    [HandleNotificationType("deviceStatus")]
    public class DeviceStatusNotificationHandler : INotificationHandler
    {
        private IDeviceRepository _deviceRepository;

        #region Constructor

        public DeviceStatusNotificationHandler(IDeviceRepository deviceRepository)
        {
            _deviceRepository = deviceRepository;
        }
        #endregion

        #region INotificationHandler Members

        public void Handle(DeviceNotification notification)
        {
            var parameters = JObject.Parse(notification.Parameters);
            var deviceStatus = (string)parameters["status"];
            if (string.IsNullOrEmpty(deviceStatus))
                throw new Exception("Device status notification is missing required 'status' parameter");

            var device = _deviceRepository.Get(notification.Device.ID);
            device.Status = deviceStatus;
            _deviceRepository.Save(device);
        }
        #endregion
    }
}