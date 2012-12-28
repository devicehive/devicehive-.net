using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeviceHive.Core.MessageLogic
{
    /// <summary>
    /// Contains names of special notifications
    /// </summary>
    public class SpecialNotifications
    {
        /// <summary>
        /// Server-originated notification about new device registration
        /// </summary>
        public const string DEVICE_ADD = "$device-add";

        /// <summary>
        /// Server-originated notification about device changes
        /// </summary>
        public const string DEVICE_UPDATE = "$device-update";

        /// <summary>
        /// Device originated notification about equipment state change
        /// </summary>
        public const string EQUIPMENT = "equipment";

        /// <summary>
        /// Device originated notification about device status change
        /// </summary>
        public const string DEVICE_STATUS = "device-status";
    }
}
