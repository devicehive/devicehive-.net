using System;
using System.IO.Ports;
using System.Linq;

namespace DeviceHive.Binary
{
	/// <summary>
	/// Base class for <see cref="IBinaryConnection"/> implementations
	/// </summary>
	public abstract class BinaryConnectionBase : IBinaryConnection
	{
	    /// <summary>
	    /// Setup binary connection to device
	    /// </summary>
	    public abstract void Connect();

	    /// <summary>
        /// Read <c>length</c> bytes from device
	    /// and returns them as byte array
	    /// </summary>
	    public abstract byte[] Read(int length);

	    /// <summary>
        /// Write <c>data</c> to device
	    /// </summary>
	    public abstract void Write(byte[] data);

	    /// <summary>
	    /// Fires when new data comes from device
	    /// </summary>
	    public event EventHandler DataAvailable;

        /// <summary>
        /// Fire <see cref="DataAvailable"/> event
        /// </summary>
		protected void OnDataAvailable(EventArgs e = null)
		{
			var handler = DataAvailable;
			if (handler != null)
				handler(this, e ?? EventArgs.Empty);
		}

	    /// <summary>
	    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	    /// </summary>
	    /// <filterpriority>2</filterpriority>
	    public virtual void Dispose()
		{
		}
	}

	/// <summary>
	/// Binary device connection through serial port
	/// </summary>
	public class SerialPortBinaryConnection : BinaryConnectionBase
	{
		private readonly SerialPort _serialPort;

		/// <summary>
		/// Initialize new instance of <see cref="SerialPortBinaryConnection"/>
		/// </summary>
		/// <param name="serialPort">
		/// <see cref="SerialPort"/> instance. <see cref="SerialPortBinaryConnection"/> takes
		/// ownership of serial port object.
		/// </param>
		public SerialPortBinaryConnection(SerialPort serialPort)
		{
			_serialPort = serialPort;
			_serialPort.DataReceived += (s, e) => OnDataAvailable();            
		}

	    /// <summary>
	    /// Setup binary connection to device through COM port
	    /// </summary>
	    public override void Connect()
	    {
            if (!_serialPort.IsOpen)
                _serialPort.Open();
	    }

	    /// <summary>
        /// Read <c>length</c> bytes from device
	    /// and returns them as byte array
	    /// </summary>
	    public override byte[] Read(int length)
		{
			var buffer = new byte[length];
			var offset = 0;

			while (offset < buffer.Length)
				offset += _serialPort.Read(buffer, offset, buffer.Length - offset);

			return buffer;
		}

	    /// <summary>
        /// Write <c>data</c> to device
	    /// </summary>
	    public override void Write(byte[] data)
		{
			_serialPort.Write(data, 0, data.Length);
		}

	    /// <summary>
	    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	    /// </summary>
	    /// <filterpriority>2</filterpriority>
	    public override void Dispose()
		{
			base.Dispose();
			_serialPort.Dispose();
		}
	}

	/// <summary>
	/// Stub <see cref="IBinaryConnection"/> implementation for testing purposes
	/// </summary>
	public class StubBinaryConnection : BinaryConnectionBase
	{
		/// <summary>
		/// Action that will be executed on <see cref="Write"/> call
		/// </summary>
		public Action<byte[]> WriteHandler { get; set; }

		private byte[] _dataToRead;

		/// <summary>
		/// Gets or sets data that will be read on <see cref="Read"/> call
		/// </summary>
		public byte[] DataToRead
		{
			get { return _dataToRead; }
			set
			{
				if (_dataToRead == value)
					return;
				
				_dataToRead = value;

				if (_dataToRead != null && _dataToRead.Length > 0)
					OnDataAvailable();
			}
		}

	    /// <summary>
	    /// Setup binary connection to device (do nothing in <see cref="StubBinaryConnection"/>)
	    /// </summary>
	    public override void Connect()
	    {
	    }

	    /// <summary>
	    /// Read length bytes from device
	    /// and returns them as byte array
	    /// </summary>
	    public override byte[] Read(int length)
		{
			var data = DataToRead.Take(length).ToArray();
			DataToRead = DataToRead.Skip(length).ToArray();
			return data;
		}

	    /// <summary>
	    /// Write data to device
	    /// </summary>
	    public override void Write(byte[] data)
		{
			var handler = WriteHandler;
			if (handler != null)
				handler(data);
		}
	}
}