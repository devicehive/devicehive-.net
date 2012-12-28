using System;
using System.Configuration;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using log4net;

namespace DeviceHive.Core.Messaging
{
    /// <summary>
    /// Represents message bus implementation using named pipes
    /// </summary>
    public class NamedPipeMessageBus : MessageBus, IDisposable
    {
        private readonly ILog _log = LogManager.GetLogger(typeof (NamedPipeMessageBus));

        private NamedPipeElement _serverPipeConfiguration;
        private NamedPipeElement[] _clientPipesConfiguration;
        private int _connectTimeout;

        private readonly Thread _readThread;
        private readonly EventWaitHandle _cancelConnectionEvent = new AutoResetEvent(false);
        private volatile bool _stopReading = false;        

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public NamedPipeMessageBus()
        {
            LoadConfiguration();

            _readThread = new Thread(ReadData);
            _readThread.Start();
        }        

        #endregion

        #region Protected Methods

        /// <summary>
        /// Sends message to all remote listeners
        /// </summary>
        /// <param name="data">Message data</param>
        protected override void SendMessage(byte[] data)
        {
            foreach (var pipeConfiguration in _clientPipesConfiguration)
            {
                using (var namedPipeClient = new NamedPipeClientStream(
                    pipeConfiguration.ServerName, pipeConfiguration.Name))
                {
                    try
                    {
                        _log.DebugFormat("Send message to {0}\\{1}",
                            pipeConfiguration.ServerName, pipeConfiguration.Name);

                        try
                        {
                            namedPipeClient.Connect(_connectTimeout);
                        }
                        catch (TimeoutException)
                        {
                            _log.WarnFormat("Couldn't connect to pipe {0}\\{1}",
                                pipeConfiguration.ServerName, pipeConfiguration.Name);
                            continue;
                        }
     
                        namedPipeClient.Write(data, 0, data.Length);
                    }
                    catch (IOException ex)
                    {
                        _log.Warn("Exception while sending a message", ex);
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private void ReadData()
        {
            var pipeSecurity = new PipeSecurity();
            var everyoneSecurityIdentifier = new SecurityIdentifier(
                WellKnownSidType.WorldSid, null);
            var everyoneAccessRule = new PipeAccessRule(everyoneSecurityIdentifier,
                PipeAccessRights.FullControl, AccessControlType.Allow);
            pipeSecurity.AddAccessRule(everyoneAccessRule);

            using (var namedPipeServer = new NamedPipeServerStream(
                _serverPipeConfiguration.Name, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous,
                0, 0, pipeSecurity))
            {
                while (true)
                {
                    if (_stopReading)
                        return;

                    ReadMessage(namedPipeServer);
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

        private void LoadConfiguration()
        {
            var configurationSection = (NamedPipeMessageBusConfigurationSection)
                ConfigurationManager.GetSection("namedPipeMessageBus");

            if (configurationSection == null)
                throw new InvalidOperationException("namedPipeMessageBus configuration sections can't be found");

            _connectTimeout = configurationSection.ConnectTimeout;

            var pipes = configurationSection.Pipes.Cast<NamedPipeElement>();
            _serverPipeConfiguration = pipes.Single(p => p.IsServer);
            _clientPipesConfiguration = pipes.ToArray();

            if (_serverPipeConfiguration.ServerName != ".")
                throw new InvalidOperationException("Server pipe can't be located on the remote machine");
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Disposes the current object
        /// </summary>
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