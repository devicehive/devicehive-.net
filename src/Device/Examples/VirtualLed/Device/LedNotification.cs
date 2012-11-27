using System;
using DeviceHive.Device;

namespace VirtualLed
{
    /// <summary>
    /// Represents notification about LED state change
    /// </summary>
    public class LedNotification : DeviceEquipmentNotification
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets new LED state.
        /// </summary>
        public int State
        {
            get { return GetParameter<int>("state"); }
            set { Parameter("state", value); }
        }
        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="equipmentCode">LED equipment code.</param>
        /// <param name="state">New LED state.</param>
        public LedNotification(string equipmentCode, int state)
            : base(equipmentCode)
        {
            State = state;
        }
        #endregion
    }
}
