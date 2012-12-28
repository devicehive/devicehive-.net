using System;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents base exception class for DeviceHive service errors.
    /// </summary>
    public class ClientServiceException : Exception
    {
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ClientServiceException()
        {
        }

        /// <summary>
        /// Initializes exception message.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public ClientServiceException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes exception message and inner exception.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public ClientServiceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        #endregion
    }
}
