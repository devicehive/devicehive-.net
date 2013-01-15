using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DeviceHive.Binary
{
	internal class MessageReaderWriter
	{
		#region Constants

		private static readonly byte[] _messageSignature = new byte[] {0xC5, 0xC3};

		private const int _headerSize = 8;

		#endregion


		private readonly IBinaryConnection _connection;

		public MessageReaderWriter(IBinaryConnection connection)
		{
			_connection = connection;
		}


		public Message ReadMessage()
		{
			var headerBytes = ReadHeaderBytes();

			byte version;
			byte flags;
			ushort dataLength;
			ushort intent;

			using (var stream = new MemoryStream(headerBytes))
			using (var reader = new BinaryReader(stream))
			{
				reader.ReadBytes(_messageSignature.Length); // skip signature
				version = reader.ReadByte();
				flags = reader.ReadByte();
				dataLength = reader.ReadUInt16();
				intent = reader.ReadUInt16();
			}

			var data = _connection.Read(dataLength);

			var checksum = _connection.Read(1)[0];
			if (checksum != CalculateChecksum(headerBytes.Concat(data)))
				throw new InvalidOperationException("Invalid message checksum");

			return new Message(version, flags, intent, data);
		}

		public void WriteMessage(Message message)
		{
			var messageLength = message.Data.Length + _headerSize;
			
			using (var memoryStream = new MemoryStream(messageLength))
			using (var writer = new BinaryWriter(memoryStream))
			{
				writer.Write(_messageSignature);
				writer.Write(message.Version);
				writer.Write(message.Flags);
				writer.Write((ushort) message.Data.Length);
				writer.Write(message.Intent);
				writer.Write(message.Data);

				var data = memoryStream.ToArray();
				var checksum = CalculateChecksum(data);

				_connection.Write(data);
				_connection.Write(new[] {checksum});
			}
		}


		private byte[] ReadHeaderBytes()
		{
			var bytes = _connection.Read(_headerSize);

			// if message is unavailable then IBinaryConnection.Read should throw exception (by timeout)
			while (true)
			{
				var signatureStart = FindSignatureStart(bytes);
				if (signatureStart == 0)
					return bytes;

				var remainingBytes = _connection.Read(signatureStart);
				bytes = bytes.Skip(signatureStart).Concat(remainingBytes).ToArray();
			}
		}

		private static int FindSignatureStart(byte[] bytes)
		{
			var index = -1;
			while (true)
			{
				index = Array.IndexOf(bytes, _messageSignature[0], index + 1);
				if (index == -1)
					return -1;

				if (index == (_headerSize - 1))
					return index;

				if (bytes[index + 1] == _messageSignature[1])
					return index;
			}
		}

		private static byte CalculateChecksum(IEnumerable<byte> bytes)
		{
			return bytes.Aggregate((b1, b2) => unchecked((byte) (b1 + b2)));
		}
	}
}