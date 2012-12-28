using System;
using Newtonsoft.Json.Linq;

namespace DeviceHive.Device
{
    /// <summary>
    /// Represents a DeviceHive command.
    /// Commands are sent by clients to devices.
    /// </summary>
    public class Command
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets command identifier (server-assigned).
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets command timestamp (server-assigned).
        /// </summary>
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets creator user identifier.
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Gets or sets command name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets command parameters.
        /// </summary>
        public JToken Parameters { get; set; }

        /// <summary>
        /// Gets or sets command execution status.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets command result (optional).
        /// </summary>
        public JToken Result { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Command()
        {
        }

        /// <summary>
        /// Initializes command name and parameters.
        /// </summary>
        /// <param name="name">Command name.</param>
        /// <param name="parameters">Command parameters.</param>
        public Command(string name, JToken parameters)
        {
            Name = name;
            Parameters = parameters;
        }

        /// <summary>
        /// Initializes all command properties.
        /// </summary>
        /// <param name="name">Command name.</param>
        /// <param name="parameters">Command parameters.</param>
        /// <param name="status">Command status.</param>
        /// <param name="result">Command result.</param>
        public Command(string name, JToken parameters, string status, JToken result)
            : this(name, parameters)
        {
            Status = status;
            Result = result;
        }
        #endregion
    }
}
