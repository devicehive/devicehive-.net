using System;

namespace DeviceHive.Core.Services
{
    /// <summary>
    /// Base class for DeviceHive services exceptions
    /// </summary>
    public abstract class ServiceException : Exception
    {
        /// <summary>
        /// Create new <see cref="ServiceException"/> with specified message
        /// </summary>
        /// <param name="message">Exception message</param>
        protected ServiceException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Thrown if client sent invalid request
    /// </summary>
    public class InvalidDataException : ServiceException
    {
        /// <summary>
        /// Creates new <see cref="InvalidDataException"/> with specified message
        /// </summary>
        /// <param name="message">Exception message</param>
        public InvalidDataException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Thrown if access to specific device network is forbidden
    /// </summary>
    public class UnauthroizedNetworkException : ServiceException
    {
        /// <summary>
        /// Creates new <see cref="UnauthroizedNetworkException"/> with specified message
        /// </summary>
        /// <param name="message">Exception message</param>
        public UnauthroizedNetworkException(string message) : base(message)
        {
        }
    }
}