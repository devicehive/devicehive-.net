using System;

namespace DeviceHive.Binary
{
	/// <summary>
	/// Abstraction of binary connection to device
	/// </summary>
	public interface IBinaryConnection : IDisposable
	{
		/// <summary>
		/// Read <see cref="length"/> bytes from device
		/// and returns them as byte array
		/// </summary>
		byte[] Read(int length);

        /// <summary>
        /// Write <see cref="data"/> to device
        /// </summary>
		void Write(byte[] data);

		/// <summary>
		/// Fires when new data comes from device
		/// </summary>
		event EventHandler DataAvailable;
	}
}