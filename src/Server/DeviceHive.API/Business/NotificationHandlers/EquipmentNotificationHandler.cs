using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;

namespace DeviceHive.API.Business.NotificationHandlers
{
    [HandleNotificationType("equipment")]
    public class EquipmentNotificationHandler : INotificationHandler
    {
        private IDeviceEquipmentRepository _deviceEquipmentRepository;

        #region Constructor

        public EquipmentNotificationHandler(IDeviceEquipmentRepository deviceEquipmentRepository)
        {
            _deviceEquipmentRepository = deviceEquipmentRepository;
        }
        #endregion

        #region INotificationHandler Members

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