using System;

namespace DeviceHive.Core.Services
{
    /// <summary>
    /// Thrown if client sent invalid request
    /// </summary>
    [Serializable]
    public class InvalidDataException : ServiceException
    {
        /// <summary>
        /// Creates new <see cref="InvalidDataException"/> with specified message
        /// </summary>
        /// <param name="message">Exception message</param>
        public InvalidDataException(string message)
            : base(message)
        {
        }
    }
}
