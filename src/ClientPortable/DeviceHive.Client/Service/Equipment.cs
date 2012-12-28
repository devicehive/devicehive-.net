using System;

namespace DeviceHive.Client
{
    /// <summary>
    /// Represents DeviceHive equipment.
    /// Equipment usually refers to various sensors that device has onboard.
    /// </summary>
    public class Equipment
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets equipment identifier (server-assigned).
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets equipment name.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets equipment code.
        /// The code is usually used in DeviceHive messages in order to refer to specific equipment.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets equipment type.
        /// The type is arbitrary string, and client may use it to make some decisions about equipment capabilities.
        /// </summary>
        public string Type { get; set; }

        #endregion
    }
}
