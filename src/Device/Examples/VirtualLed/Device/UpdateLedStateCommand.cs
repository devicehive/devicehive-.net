using System;
using System.Collections.Generic;
using DeviceHive.Device;

namespace VirtualLed
{
    /// <summary>
    /// Represents a command requesting to change LED state
    /// </summary>
    public class UpdateLedStateCommand
    {
        #region Public Properties

        /// <summary>
        /// Gets LED equipment code.
        /// </summary>
        public string Equipment { get; set; }

        /// <summary>
        /// Gets new LED state.
        /// </summary>
        public int? State { get; set; }

        #endregion
    }
}
