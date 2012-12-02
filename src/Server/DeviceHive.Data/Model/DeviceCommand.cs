using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DeviceHive.Data.Validation;

namespace DeviceHive.Data.Model
{
    /// <summary>
    /// Represents a device command, a unit of information sent to devices.
    /// </summary>
    public class DeviceCommand
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public DeviceCommand()
        {
        }

        /// <summary>
        /// Initializes all required properties
        /// </summary>
        /// <param name="command">Command text</param>
        /// <param name="device">Associated device object</param>
        public DeviceCommand(string command, Device device)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentException("Command is null or empty!", "command");
            if (device == null)
                throw new ArgumentNullException("device");

            this.Command = command;
            this.Device = device;
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Command identifier.
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// Command timestamp (UTC).
        /// </summary>
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// Command name.
        /// </summary>
        [Required]
        [StringLength(128)]
        public string Command { get; set; }

        /// <summary>
        /// Command parameters, a JSON object with an arbitrary structure.
        /// </summary>
        [JsonField]
        public string Parameters { get; set; }

        /// <summary>
        /// Command lifetime, a number of seconds until this command expires.
        /// </summary>
        public int? Lifetime { get; set; }

        /// <summary>
        /// Command flags, and optional value that could be supplied for device or related infrastructure.
        /// </summary>
        public int? Flags { get; set; }

        /// <summary>
        /// Command status, as reported by device or related infrastructure.
        /// </summary>
        [StringLength(128)]
        public string Status { get; set; }

        /// <summary>
        /// Command execution result, and optional value that could be provided by device.
        /// </summary>
        [StringLength(1024)]
        public string Result { get; set; }

        /// <summary>
        /// Associated device identifier.
        /// </summary>
        public int DeviceID { get; set; }

        /// <summary>
        /// Associated device object.
        /// </summary>
        [Required]
        public Device Device { get; set; }

        #endregion
    }
}
