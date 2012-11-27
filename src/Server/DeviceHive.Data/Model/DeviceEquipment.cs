using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DeviceHive.Data.Validation;

namespace DeviceHive.Data.Model
{
    /// <summary>
    /// Represents a device equipment state
    /// </summary>
    public class DeviceEquipment
    {
        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public DeviceEquipment()
        {
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="code">Equipment code</param>
        /// <param name="timestamp">Equipment state timestamp</param>
        /// <param name="device">Associated device object</param>
        public DeviceEquipment(string code, DateTime timestamp, Device device)
        {
            if (string.IsNullOrEmpty(code))
                throw new ArgumentException("Code is null or empty!", "code");
            if (device == null)
                throw new ArgumentNullException("device");

            this.Code = code;
            this.Timestamp = timestamp;
            this.Device = device;
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Record identifier.
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// Equipment code.
        /// </summary>
        [Required]
        [StringLength(128)]
        public string Code { get; set; }

        /// <summary>
        /// Equipment state timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Current equipment state.
        /// </summary>
        [JsonField]
        public string Parameters { get; set; }

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
