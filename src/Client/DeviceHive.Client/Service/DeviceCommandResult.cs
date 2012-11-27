using System;

namespace DeviceHive.Client
{
    /// <summary>
    /// DeviceCommand result structure.
    /// </summary>
    /// <remarks>
    /// The class is to describe device command's results.
    /// </remarks>
    public class DeviceCommandResult
    {
        #region Public Properties

        /// <summary>
        /// This is a status of the device command.
        /// </summary>
        public string Status { get; private set; }

        /// <summary>
        /// This is a result of the device command.
        /// </summary>
        public string Result { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor of the class.
        /// </summary>
        /// <param name="status">Initializes Status property only</param>
        public DeviceCommandResult(string status)
            : this(status, null)
        {
        }

        /// <summary>
        /// Constructor of the class.
        /// </summary>
        /// <param name="status">Initializes Status property</param>
        /// <param name="result">Initializes Result property</param>
        public DeviceCommandResult(string status, string result)
        {
            if (string.IsNullOrEmpty(status))
                throw new ArgumentException("Status is null or empty!", "status");

            Status = status;
            Result = result;
        }
        #endregion
    }
}
