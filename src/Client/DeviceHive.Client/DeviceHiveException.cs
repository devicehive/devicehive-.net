using System;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents base exception class for DeviceHive client errors.
    /// </summary>
#if !PORTABLE && !NETFX_CORE
    [Serializable]
#endif
    public class DeviceHiveException : Exception
    {
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DeviceHiveException()
        {
        }

        /// <summary>
        /// Initializes exception message.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public DeviceHiveException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes exception message and inner exception.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public DeviceHiveException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        #endregion
    }

    /// <summary>
    /// Represents an exception class for DeviceHive authorization errors.
    /// </summary>
#if !PORTABLE && !NETFX_CORE
    [Serializable]
#endif
    public class DeviceHiveUnauthorizedException : DeviceHiveException
    {
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DeviceHiveUnauthorizedException()
        {
        }

        /// <summary>
        /// Initializes exception message.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public DeviceHiveUnauthorizedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes exception message and inner exception.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public DeviceHiveUnauthorizedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        #endregion
    }
}
