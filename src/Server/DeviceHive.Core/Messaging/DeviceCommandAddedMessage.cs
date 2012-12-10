using System;

namespace DeviceHive.Core.Messaging
{
    /// <summary>
    /// A message about new device command being added
    /// </summary>
    public class DeviceCommandAddedMessage
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
        public DeviceCommandAddedMessage()
        {
        }

        /// <summary>
        /// Specifies device and command identifiers
        /// </summary>
        /// <param name="DeviceId">Device identifier</param>
        /// <param name="commandId">Command identifier</param>
        public DeviceCommandAddedMessage(int deviceId, int commandId)
        {
            DeviceId = deviceId;
            CommandId = commandId;
        }
        #endregion
    }
}
