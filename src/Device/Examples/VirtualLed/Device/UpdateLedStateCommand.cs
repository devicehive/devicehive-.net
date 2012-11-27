using System;
using System.Collections.Generic;
using DeviceHive.Device;

namespace VirtualLed
{
    /// <summary>
    /// Represents a command requesting to change LED state
    /// </summary>
    public class UpdateLedStateCommand : DeviceCommand
    {
        #region Public Properties

        /// <summary>
        /// Gets LED equipment code.
        /// </summary>
        public string Equipment
        {
            get { return GetParameter("equipment"); }
        }

        /// <summary>
        /// Gets new LED state.
        /// </summary>
        public int? State
        {
            get { return GetParameter<int?>("state"); }
        }
        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">Command name.</param>
        /// <param name="parameters">Dictionary of command parameters.</param>
        public UpdateLedStateCommand(string name, Dictionary<string, string> parameters)
            : base(name, parameters)
        {
        }
        #endregion
    }
}
