using System;
using DeviceHive.Device;

namespace VirtualLed
{
    /// <summary>
    /// Represents notification about LED state change
    /// </summary>
    public class LedNotification
    {
        #region Public Properties

        /// <summary>
        /// Gets new LED state.
        /// </summary>
        [Parameter("state")]
        public int State { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="state">New LED state.</param>
        public LedNotification(int state)
        {
            State = state;
        }
        #endregion
    }
}
