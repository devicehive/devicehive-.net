namespace DeviceHive.Core.MessageLogic
{
    /// <summary>
    /// Represents information about a message handler.
    /// </summary>
    internal class MessageHandlerInfo
    {
        #region Public Properties

        /// <summary>
        /// Gets message handler instance.
        /// </summary>
        public MessageHandler MessageHandler { get; private set; }

        /// <summary>
        /// Gets array of notification names to handle.
        /// </summary>
        public string[] NotificationNames { get; private set; }

        /// <summary>
        /// Gets array of command names to handle.
        /// </summary>
        public string[] CommandNames { get; private set; }

        /// <summary>
        /// Gets array of device guids to handle.
        /// </summary>
        public string[] DeviсeGuids { get; private set; }

        /// <summary>
        /// Gets array of device class ids to handle.
        /// </summary>
        public int[] DeviсeClassIds { get; private set; }

        /// <summary>
        /// Gets array of network ids to handle.
        /// </summary>
        public int[] NetworkIds { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="messageHandler">MessageHandler object.</param>
        /// <param name="notificationNames">Array of notification names to handle.</param>
        /// <param name="commandNames">Array of command names to handle.</param>
        /// <param name="deviceGuids">Array of device guids to handle.</param>
        /// <param name="deviceClassIds">Array of device class ids to handle.</param>
        /// <param name="networkIds">Array of network ids to handle.</param>
        public MessageHandlerInfo(MessageHandler messageHandler, string[] notificationNames, string[] commandNames,
            string[] deviceGuids, int[] deviceClassIds, int[] networkIds)
        {
            MessageHandler = messageHandler;
            NotificationNames = notificationNames;
            CommandNames = commandNames;
            DeviсeGuids = deviceGuids;
            DeviсeClassIds = deviceClassIds;
            NetworkIds = networkIds;
        }
        #endregion
    }
}
