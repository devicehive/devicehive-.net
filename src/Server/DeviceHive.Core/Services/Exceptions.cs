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
    public class BadRequestException : ServiceException
    {
        /// <summary>
        /// Creates new <see cref="BadRequestException"/> with specified message
        /// </summary>
        /// <param name="message">Exception message</param>
        public BadRequestException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Thrown if access to specific resource is forbidden
    /// </summary>
    public class AccessForbiddenException : ServiceException
    {
        /// <summary>
        /// Creates new <see cref="AccessForbiddenException"/> with specified message
        /// </summary>
        /// <param name="message">Exception message</param>
        public AccessForbiddenException(string message) : base(message)
        {
        }
    }
}