using System;
using Newtonsoft.Json.Linq;

namespace DeviceHive.Device
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

        /// <summary>
        /// Gets or sets associated equipment data.
        /// </summary>
        public JToken Data { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Equipment()
        {
        }

        /// <summary>
        /// Initializes equipment name, code and type properties.
        /// </summary>
        /// <param name="name">Equipment name</param>
        /// <param name="code">Equipment code. The code is usually used in DeviceHive messages in order to refer to specific equipment.</param>
        /// <param name="type">Equipment type. The type is arbitrary string, and client may use it to make some decisions about equipment capabilities.</param>
        public Equipment(string name, string code, string type)
        {
            Name = name;
            Code = code;
            Type = type;
        }

        /// <summary>
        /// Initializes all equipment properties.
        /// </summary>
        /// <param name="name">Equipment name</param>
        /// <param name="code">Equipment code. The code is usually used in DeviceHive messages in order to refer to specific equipment.</param>
        /// <param name="type">Equipment type. The type is arbitrary string, and client may use it to make some decisions about equipment capabilities.</param>
        /// <param name="data">Equipment data, an optional json token used to describe additional equipment information.</param>
        public Equipment(string name, string code, string type, JToken data)
            : this(name, code, type)
        {
            Data = data;
        }
        #endregion
    }
}
