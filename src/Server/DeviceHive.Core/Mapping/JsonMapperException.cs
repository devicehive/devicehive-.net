using System;

namespace DeviceHive.Core.Mapping
{
    /// <summary>
    /// Represents json mapping exception
    /// </summary>
    [Serializable]
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