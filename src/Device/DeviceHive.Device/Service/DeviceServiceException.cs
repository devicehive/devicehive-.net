using System;

namespace DeviceHive.Device
{
    /// <summary>
    /// Represents base exception class for DeviceHive service errors.
    /// </summary>
    public class DeviceServiceException : Exception
    {
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DeviceServiceException()
        {
        }

        /// <summary>
        /// Initializes exception message.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public DeviceServiceException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes exception message and inner exception.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public DeviceServiceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        #endregion
    }
}
