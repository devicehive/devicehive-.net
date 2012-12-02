using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;

namespace DeviceHive.Core.MessageLogic.NotificationHandlers
{
    /// <summary>
    /// Handles device status notifications.
    /// The handler update device status in the datastore.
    /// </summary>
    public class DeviceStatusNotificationHandler : INotificationHandler
    {
        private readonly IDeviceRepository _deviceRepository;

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="deviceRepository">IDeviceRepository implementation</param>
        public DeviceStatusNotificationHandler(IDeviceRepository deviceRepository)
        {
            _deviceRepository = deviceRepository;
        }
        #endregion

        #region INotificationHandler Members

        /// <summary>
        /// Gets array of supported notification types
        /// </summary>
        public string[] NotificationTypes
        {
            get { return new[] { "deviceStatus" }; }
        }

        /// <summary>
        /// Handles a device notification
        /// </summary>
        /// <param name="notification">DeviceNotification object</param>
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