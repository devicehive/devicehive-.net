using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DeviceHive.Data.Validation;
using System.ComponentModel;

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
        /// Command execution result, an optional value that could be provided by device.
        /// </summary>
        [JsonField]
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

        /// <summary>
        /// Associated user identifier.
        /// </summary>
        public int? UserID { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents a device command filter.
    /// </summary>
    public class DeviceCommandFilter
    {
        #region Public Properties

        /// <summary>
        /// Filter by command start timestamp (UTC).
        /// </summary>
        public DateTime? Start { get; set; }

        /// <summary>
        /// Filter by command end timestamp (UTC).
        /// </summary>
        public DateTime? End { get; set; }

        /// <summary>
        /// Gets or sets flag indicating if start and end dates are inclusive (default is true).
        /// </summary>
        public bool IsDateInclusive { get; set; }

        /// <summary>
        /// Filter by command names.
        /// </summary>
        public string[] Commands { get; set; }

        /// <summary>
        /// Filter by command name.
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// Filter by command status.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Result list sort field. Available values are Timestamp (default), Command and Status.
        /// </summary>
        [DefaultValue(DeviceCommandSortField.Timestamp)]
        public DeviceCommandSortField SortField { get; set; }

        /// <summary>
        /// Result list sort order. Available values are ASC and DESC.
        /// </summary>
        [DefaultValue(SortOrder.ASC)]
        public SortOrder SortOrder { get; set; }

        /// <summary>
        /// Number of records to skip from the result list.
        /// </summary>
        public int? Skip { get; set; }

        /// <summary>
        /// Number of records to take from the result list (default is 1000).
        /// </summary>
        public int? Take { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DeviceCommandFilter()
        {
            IsDateInclusive = true;
            SortField = DeviceCommandSortField.Timestamp;
            Take = 1000;
        }
        #endregion
    }

    /// <summary>
    /// Represents device command sort fields.
    /// </summary>
    public enum DeviceCommandSortField
    {
        None = 0,
        Timestamp = 1,
        Command = 2,
        Status = 3
    }
}
