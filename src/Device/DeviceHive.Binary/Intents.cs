namespace DeviceHive.Binary
{
    /// <summary>
    /// Intents for DeviceHive binary protocol messages
    /// </summary>
    public static class Intents
	{
		/// <summary>
		/// Request registration from device
		/// </summary>
		public const ushort RequestRegistration = 0;

        /// <summary>
        /// Register device in the DeviceHive
        /// </summary>
		public const ushort Register = 1;

        /// <summary>
        /// Notify DeviceHive about device command execution result
        /// </summary>
		public const ushort NotifyCommandResult = 2;

        /// <summary>
        /// Register device in the DeviceHive (JSON request)
        /// </summary>
        public const ushort Register2 = 3;

        /// <summary>
        /// Start value for custom intents
        /// </summary>
        public const ushort User = 256;
	}
}