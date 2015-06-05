using System;

namespace DeviceHive.Core.Services
{
    /// <summary>
    /// Base class for DeviceHive services exceptions
    /// </summary>
    [Serializable]
    public abstract class ServiceException : Exception
    {
        /// <summary>
        /// Create new <see cref="ServiceException"/> with specified message
        /// </summary>
        /// <param name="message">Exception message</param>
        protected ServiceException(string message)
            : base(message)
        {
        }
    }
}
