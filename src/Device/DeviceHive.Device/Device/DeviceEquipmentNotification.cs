using System;

namespace DeviceHive.Device
{
    /// <summary>
    /// Represents a notification sent from <see cref="DeviceBase"/> descendants to update equipment state in the DeviceHive service.
    /// </summary>
    /// <remarks>
    /// This is the base class for notifications sent by devices that include information about device equipment changes.
    /// These notifications allow DeviceHive to track the latest state of the equipment so clients can retrieve it from the service.
    /// </remarks>
    public class DeviceEquipmentNotification : DeviceNotification
    {
        #region Public Properties

        /// <summary>
        /// Gets equipment code as set in <see cref="DeviceEquipmentInfo.Code"/> property.
        /// </summary>
        public string EquipmentCode
        {
            get { return Parameters["equipment"]; }
            private set { Parameters["equipment"] = value; }
        }
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes equipment code.
        /// </summary>
        /// <param name="equipmentCode">Equipment code as set in <see cref="DeviceEquipmentInfo.Code"/> property.</param>
        public DeviceEquipmentNotification(string equipmentCode)
            : base("equipment")
        {
            if (string.IsNullOrEmpty(equipmentCode))
                throw new ArgumentException("Equipment code is null or empty!", "equipmentCode");

            EquipmentCode = equipmentCode;
        }
        #endregion
    }
}
