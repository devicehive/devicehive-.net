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
        [Parameter("equipment")]
        public string Equipment { get; private set; }

        /// <summary>
        /// Gets new LED state.
        /// </summary>
        [Parameter("state")]
        public int? State { get; private set; }

        #endregion
    }
}
