using System;

namespace DeviceHive.API.Mapping
{
    /// <summary>
    /// Represents json mapping exception
    /// </summary>
    public class JsonMapperException : Exception
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public JsonMapperException()
        {
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="message">Exception message</param>
        public JsonMapperException(string message)
            : base(message)
        {
        }
        #endregion
    }
}