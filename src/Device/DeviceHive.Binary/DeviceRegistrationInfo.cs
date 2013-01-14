using System;

namespace DeviceHive.Binary
{
	/// <summary>
	/// Represent information that is used on device registration
	/// </summary>
	public class DeviceRegistrationInfo
	{
		/// <summary>
		/// Gets or sets device unique identifier
		/// </summary>
		public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets device key
        /// </summary>
		public string Key { get; set; }

        /// <summary>
        /// Gets or sets device name
        /// </summary>
		public string Name { get; set; }

        /// <summary>
        /// Gets or sets device class name
        /// </summary>
		public string ClassName { get; set; }

        /// <summary>
        /// Gets or sets device class version
        /// </summary>
		public string ClassVersion { get; set; }

        /// <summary>
        /// Gets or sets device equipment items list
        /// </summary>
		public EquipmentInfo[] Equipment { get; set; }

        /// <summary>
        /// Gets or sets supported device notification list
        /// </summary>
		public NotificationInfo[] Notifications { get; set; }

        /// <summary>
        /// Gets or sets supported device command list
        /// </summary>
		public CommandInfo[] Commands { get; set; }
	}

	/// <summary>
    /// Represent information about device equipment
	/// </summary>
	public class EquipmentInfo
	{
		/// <summary>
		/// Gets or sets equipment name
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets equipment code
		/// </summary>
		public string Code { get; set; }

		/// <summary>
		/// Gets or sets equipment type name
		/// </summary>
		public string TypeName { get; set; }
	}

	/// <summary>
    /// Represent information about supported device notification
	/// </summary>
	public class NotificationInfo
	{
		/// <summary>
		/// Gets or sets intent value that will be used in binary protocol messages
		/// </summary>
		public ushort Intent { get; set; }

		/// <summary>
		/// Gets or sets notification name
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets supported notification parameters list
		/// </summary>
		public ParameterInfo[] Parameters { get; set; }
	}

    /// <summary>
    /// Represent information about supported device command
    /// </summary>
	public class CommandInfo
	{
        /// <summary>
        /// Gets or sets intent value that will be used in binary protocol messages
        /// </summary>
		public ushort Intent { get; set; }

        /// <summary>
        /// Gets or sets command name
        /// </summary>
		public string Name { get; set; }

        /// <summary>
        /// Gets or sets supported command parameters list
        /// </summary>
		public ParameterInfo[] Parameters { get; set; }
	}

	/// <summary>
	/// Represent information about parameters for device command or notification
	/// </summary>
	public class ParameterInfo
	{
		/// <summary>
		/// Gets or sets parameter data type
		/// </summary>
		public DataType DataType { get; set; }

		/// <summary>
		/// Gets or sets parameter name
		/// </summary>
		public string Name { get; set; }
	}
}