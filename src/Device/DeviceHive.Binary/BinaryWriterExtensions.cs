using System;
using System.IO;
using System.Text;

namespace DeviceHive.Binary
{
    internal static class BinaryWriterExtensions
    {
        public static void WriteGuid(this BinaryWriter writer, Guid guid)
        {
            var bytes = guid.ToByteArray();

            // a
            writer.Write(bytes[3]);
            writer.Write(bytes[2]);
            writer.Write(bytes[1]);
            writer.Write(bytes[0]);

            // b
            writer.Write(bytes[5]);
            writer.Write(bytes[4]);

            // c
            writer.Write(bytes[7]);
            writer.Write(bytes[6]);

            // d
            writer.Write(bytes, 8, 8);
        }

        public static void WriteUtfString(this BinaryWriter writer, string str)
        {
            writer.Write((ushort) str.Length);
            writer.Write(Encoding.UTF8.GetBytes(str));
        }

        public static void WriteBinary(this BinaryWriter writer, byte[] data)
        {
            writer.Write((ushort)data.Length);
            writer.Write(data);
        }
    }
}