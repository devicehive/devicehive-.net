using System;

namespace DeviceHive.Core.Messaging
{
    /// <summary>
    /// Message about a device command being updated
    /// </summary>
    public class DeviceCommandUpdatedMessage
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets device identifier
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// Gets or sets command identifier
        /// </summary>
        public int CommandId { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public DeviceCommandUpdatedMessage()
        {
        }

        /// <summary>
        /// Specifies device and command identifiers
        /// </summary>
        /// <param name="deviceId">Device identifier</param>
        /// <param name="commandId">Command identifier</param>
        public DeviceCommandUpdatedMessage(int deviceId, int commandId)
        {
            DeviceId = deviceId;
            CommandId = commandId;
        }
        #endregion
    }
}
