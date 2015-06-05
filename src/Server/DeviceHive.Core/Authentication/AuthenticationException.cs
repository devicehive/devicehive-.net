using System;

namespace DeviceHive.Core.Authentication
{
    /// <summary>
    /// Represents authentication exception.
    /// </summary>
    [Serializable]
    public class AuthenticationException : Exception
    {
        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AuthenticationException()
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public AuthenticationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public AuthenticationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        #endregion
    }
}
