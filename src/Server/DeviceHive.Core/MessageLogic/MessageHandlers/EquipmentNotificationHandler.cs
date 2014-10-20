using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using DeviceHive.Data.Model;
using DeviceHive.Data.Repositories;

namespace DeviceHive.Core.MessageLogic.MessageHandlers
{
    /// <summary>
    /// Handles equipment notifications.
    /// The handler update equipment state in the datastore.
    /// </summary>
    public class EquipmentMessageHandler : MessageHandler
    {
        private readonly IDeviceEquipmentRepository _deviceEquipmentRepository;

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="deviceEquipmentRepository">IDeviceEquipmentRepository implementation</param>
        public EquipmentMessageHandler(IDeviceEquipmentRepository deviceEquipmentRepository)
        {
            _deviceEquipmentRepository = deviceEquipmentRepository;
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
            var equipmentCode = (string)parameters["equipment"];
            if (string.IsNullOrEmpty(equipmentCode))
                throw new Exception("Equipment notification is missing required 'equipment' parameter");

            parameters.Remove("equipment");
            var equipment = _deviceEquipmentRepository.GetByDeviceAndCode(context.Device.ID, equipmentCode);
            if (equipment == null)
            {
                equipment = new DeviceEquipment(equipmentCode, context.Notification.Timestamp, context.Device);
            }
            equipment.Timestamp = context.Notification.Timestamp;
            equipment.Device = context.Device;
            equipment.Parameters = parameters.ToString();
            _deviceEquipmentRepository.Save(equipment);
        }
        #endregion
    }
}