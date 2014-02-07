using System;

namespace DeviceHive.Core.Services
{
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