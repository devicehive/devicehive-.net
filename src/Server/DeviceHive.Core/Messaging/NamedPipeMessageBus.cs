using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using log4net;

namespace DeviceHive.Core.Messaging
{
    /// <summary>
    /// Represents message bus implementation using named pipes
    /// </summary>
    public class NamedPipeMessageBus : MessageBus, IDisposable
    {
        private readonly string _pipeName;
        private readonly Thread _readThread;
        private readonly EventWaitHandle _cancelConnectionEvent = new AutoResetEvent(false);
        private NamedPipeServerStream _namedPipeServer;
        private volatile bool _stopReading = false;

        #region Constructor

        /// <summary>
        /// Specified named pipe name
        /// </summary>
        /// <param name="pipeName">Named pipe name</param>
        public NamedPipeMessageBus(string pipeName)
        {
            _pipeName = pipeName;

            _readThread = new Thread(ReadData);
            _readThread.Start();
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public NamedPipeMessageBus()
            : this("DeviceHive")
        {            
        }
        #endregion

        #region Protected Methods

        /// <summary>
        /// Sends message to all remote listeners
        /// </summary>
        /// <param name="data">Message data</param>
        protected override void SendMessage(byte[] data)
        {            
            var serversNotified = 0;

            while (true)
            {
                using (var namedPipeClient = new NamedPipeClientStream(_pipeName))
                {
                    try
                    {
                        namedPipeClient.Connect();
                        namedPipeClient.Write(data, 0, data.Length);

                        if (++serversNotified >= namedPipeClient.NumberOfServerInstances)
                            break;
                    }
                    catch (IOException ex)
                    {
                        LogManager.GetLogger(GetType()).Warn("Exception while sending a message", ex);
                    }
                }
            }
        }
        #endregion

        #region Private Methods

        private void ReadData()
        {
            using (_namedPipeServer = new NamedPipeServerStream(_pipeName,
                PipeDirection.InOut, -1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                while (true)
                {
                    if (_stopReading)
                        return;
                    
                    ReadMessage(_namedPipeServer);                    
                }
            }
        }

        private void ReadMessage(NamedPipeServerStream namedPipeServer)
        {
            var connectEvent = new AutoResetEvent(false);

            var connectResult = namedPipeServer.BeginWaitForConnection(ar => connectEvent.Set(), null);
            WaitHandle.WaitAny(new WaitHandle[] {_cancelConnectionEvent, connectEvent});

            if (_stopReading)
                return;

            namedPipeServer.EndWaitForConnection(connectResult);            

            byte[] data;
            
            using (var ms = new MemoryStream())
            {
                var buffer = new byte[256 * 1024];

                int bytesRead;
                while ((bytesRead = namedPipeServer.Read(buffer, 0, buffer.Length)) > 0)
                    ms.Write(buffer, 0, bytesRead);

                data = ms.ToArray();
            }

            namedPipeServer.Disconnect();

            HandleMessage(data);
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (_readThread != null && _readThread.IsAlive)
            {
                _stopReading = true;
                _cancelConnectionEvent.Set();
                _readThread.Join();
            }
        }

        #endregion
    }
}