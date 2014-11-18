using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;

namespace DeviceHive.Core.MessageLogic.MessageHandlers
{
    /// <summary>
    /// Handles device status notifications.
    /// The handler update device status in the datastore.
    /// </summary>
    public class DeviceStatusMessageHandler : MessageHandler
    {
        private readonly IDeviceRepository _deviceRepository;

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="deviceRepository">IDeviceRepository implementation</param>
        public DeviceStatusMessageHandler(IDeviceRepository deviceRepository)
        {
            _deviceRepository = deviceRepository;
        }
        #endregion

        #region MessageHandler Members

        /// <summary>
        /// Invoked when a new notification is inserted.
        /// </summary>
        /// <param name="context">MessageHandlerContext object.</param>
        public override void NotificationInserted(MessageHandlerContext context)
        {
            var parameters = JObject.Parse(context.Notification.Parameters);
            var deviceStatus = (string)parameters["status"];
            if (string.IsNullOrEmpty(deviceStatus))
                throw new Exception("Device status notification is missing required 'status' parameter");

            var device = _deviceRepository.Get(context.Device.ID);
            device.Status = deviceStatus;
            _deviceRepository.Save(device);
        }
        #endregion
    }
}