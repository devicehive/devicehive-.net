using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeviceHive.API.Models
{
    /// <summary>
    /// Represents error detail
    /// </summary>
    [JsonObject]
    public class ErrorDetail
    {
        #region Public Properties

        /// <summary>
        /// Gets error code
        /// </summary>
        [JsonProperty("error")]
        public int? Error { get; private set; }

        /// <summary>
        /// Gets error message
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="message">Error message</param>
        public ErrorDetail(string message)
        {
            Message = message;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="error">Error code</param>
        /// <param name="message">Error message</param>
        public ErrorDetail(int error, string message)
        {
            Error = error;
            Message = message;
        }
        #endregion
    }
}