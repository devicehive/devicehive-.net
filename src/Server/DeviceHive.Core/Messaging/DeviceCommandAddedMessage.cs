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
        /// Gets or sets device unique identifier
        /// </summary>
        public Guid DeviceGuid { get; set; }

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
        /// <param name="deviceGuid">Device unique identifier</param>
        /// <param name="commandId">Command identifier</param>
        public DeviceCommandAddedMessage(Guid deviceGuid, int commandId)
        {
            DeviceGuid = deviceGuid;
            CommandId = commandId;
        }
        #endregion
    }
}
