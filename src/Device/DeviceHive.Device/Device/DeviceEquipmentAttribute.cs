using System;

namespace DeviceHive.Device
{
    /// <summary>
    /// DeviceEquipmentAttribute set on descendants of the <see cref="DeviceBase"/> class to specify associated device class meta-information.
    /// </summary>
    /// <remarks>
    /// Equipment attributes describe various sensors device have onboard purely for meta-information purposes.
    /// Alternatively, equipment could be defined in device object by overriding <see cref="DeviceBase.EquipmentInfo"/> property.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DeviceEquipmentAttribute : Attribute
    {
        #region Public Properties

        /// <summary>
        /// Gets equipment code.
        /// The code is usually used in DeviceHive messages in order to refer to specific equipment.
        /// </summary>
        public string Code { get; private set; }

        /// <summary>
        /// Gets equipment name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets equipment type.
        /// The type is arbitrary string, and client may use it to make some decisions about equipment capabilities.
        /// </summary>
        public string Type { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes equipment code, name and type.
        /// </summary>
        /// <param name="code">Equipment code. The code is usually used in DeviceHive messages in order to refer to specific equipment.</param>
        /// <param name="name">Equipment name.</param>
        /// <param name="type">Equipment type. The type is arbitrary string, and client may use it to make some decisions about equipment capabilities.</param>
        public DeviceEquipmentAttribute(string code, string name, string type)
        {
            if (string.IsNullOrEmpty(code))
                throw new ArgumentException("Code is null or empty", "code");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name is null or empty", "name");
            if (string.IsNullOrEmpty(type))
                throw new ArgumentException("Type is null or empty", "type");

            Code = code;
            Name = name;
            Type = type;
        }
        #endregion
    }
}
