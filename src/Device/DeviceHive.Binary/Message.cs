namespace DeviceHive.Binary
{
	internal class Message
	{
		private readonly byte _version;
		private readonly byte _flags;
		private readonly ushort _intent;
		private readonly byte[] _data;

		public Message(byte version, byte flags, ushort intent, byte[] data)
		{
			_version = version;
			_flags = flags;
			_intent = intent;
			_data = data;
		}

		public byte Version
		{
			get { return _version; }
		}

		public byte Flags
		{
			get { return _flags; }
		}

		public ushort Intent
		{
			get { return _intent; }
		}

		public byte[] Data
		{
			get { return _data; }
		}
	}
}