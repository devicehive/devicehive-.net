using System;
using System.IO;
using System.Text;

namespace DeviceHive.Binary
{
	internal static class BinaryReaderExtensions
	{
		public static Guid ReadGuid(this BinaryReader reader)
		{
			var bytes = reader.ReadBytes(16);
			
			var a = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[0];
			var b = (short) ((bytes[4] << 8) | bytes[5]);
			var c = (short)((bytes[6] << 8) | bytes[7]);
			
			var d = new byte[8];
			Array.Copy(bytes, 8, d, 0, 8);

			return new Guid(a, b, c, d);
		}

		public static string ReadUtfString(this BinaryReader reader)
		{
			var length = reader.ReadUInt16();
			var data = reader.ReadBytes(length);
			return Encoding.UTF8.GetString(data);
		}

		public static byte[] ReadBinary(this BinaryReader reader)
		{
			var length = reader.ReadUInt16();
			return reader.ReadBytes(length);
		}

		public static T[] ReadArray<T>(this BinaryReader binaryReader, Func<BinaryReader, T> itemReader)
		{
			var length = binaryReader.ReadUInt16();
			var items = new T[length];

			for (var i = 0; i < length; i++)
				items[i] = itemReader(binaryReader);

			return items;
		}
	}
}