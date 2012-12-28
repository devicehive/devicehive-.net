using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;

namespace DeviceHive.Core.MessageLogic.NotificationHandlers
{
    /// <summary>
    /// Handles equipment notifications.
    /// The handler update equipment state in the datastore.
    /// </summary>
    public class EquipmentNotificationHandler : INotificationHandler
    {
        private readonly IDeviceEquipmentRepository _deviceEquipmentRepository;

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="deviceEquipmentRepository">IDeviceEquipmentRepository implementation</param>
        public EquipmentNotificationHandler(IDeviceEquipmentRepository deviceEquipmentRepository)
        {
            _deviceEquipmentRepository = deviceEquipmentRepository;
        }
        #endregion

        #region INotificationHandler Members

        /// <summary>
        /// Gets array of supported notification types
        /// </summary>
        public string[] NotificationTypes
        {
            get { return new[] { SpecialNotifications.EQUIPMENT }; }
        }

        /// <summary>
        /// Handles a device notification
        /// </summary>
        /// <param name="notification">DeviceNotification object</param>
        public void Handle(DeviceNotification notification)
        {
            var parameters = JObject.Parse(notification.Parameters);
            var equipmentCode = (string)parameters["equipment"];
            if (string.IsNullOrEmpty(equipmentCode))
                throw new Exception("Equipment notification is missing required 'equipment' parameter");

            parameters.Remove("equipment");
            var equipment = _deviceEquipmentRepository.GetByDeviceAndCode(notification.Device.ID, equipmentCode);
            if (equipment == null)
            {
                equipment = new DeviceEquipment(equipmentCode, notification.Timestamp, notification.Device);
            }
            equipment.Timestamp = notification.Timestamp;
            equipment.Device = notification.Device;
            equipment.Parameters = parameters.ToString();
            _deviceEquipmentRepository.Save(equipment);
        }
        #endregion
    }
}