using System;
using System.Collections.Generic;
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
        private const int _connectRetryTimes = 5;

        private readonly ILog _log = LogManager.GetLogger(typeof (NamedPipeMessageBus));

        private NamedPipeElement _serverPipeConfiguration;
        private NamedPipeElement[] _clientPipesConfiguration;
        private int _connectTimeout = 100;

        private readonly Thread _readThread;
        private readonly EventWaitHandle _cancelConnectionEvent = new ManualResetEvent(false);
        private volatile bool _stopReading = false;

        #region Constructors

        private NamedPipeMessageBus(Configuration configuration)
        {
            LoadConfiguration(configuration);

            _readThread = new Thread(ReadData);
            _readThread.Start();
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public NamedPipeMessageBus() : this(GetConfiguration())
        {            
        }

        /// <summary>
        /// Initialize new instance of <see cref="NamedPipeMessageBus"/> for two way communication
        /// </summary>
        public NamedPipeMessageBus(string serverPipeName, string clientPipeName) :
            this(new Configuration(serverPipeName, clientPipeName))
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
            foreach (var pipeConfiguration in _clientPipesConfiguration)
            {
                using (var namedPipeClient = new NamedPipeClientStream(
                    pipeConfiguration.ServerName, pipeConfiguration.Name))
                {
                    try
                    {
                        _log.DebugFormat("Send message to {0}\\{1}",
                            pipeConfiguration.ServerName, pipeConfiguration.Name);

                        if (!ConnectToPipe(namedPipeClient))
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

        private bool ConnectToPipe(NamedPipeClientStream namedPipeClient)
        {
            for (int i = 0; i < _connectRetryTimes; i++)
            {
                try
                {
                    namedPipeClient.Connect(_connectTimeout);
                    return true;
                }
                catch (IOException ex) // pipe is in use
                {
                    // retry one more time
                    _log.WarnFormat("Exception on connect to pipe (pipe is in use): {0}", ex);                    
                }
                catch (TimeoutException ex) // pipe doesn't exist
                {
                    _log.WarnFormat("Exception on connect to pipe (pipe doesn't exist): {0}", ex);
                    return false;
                }
            }

            return false;
        }

        private void ReadData()
        {
            var pipeSecurity = new PipeSecurity();
            var everyoneSecurityIdentifier = new SecurityIdentifier(
                WellKnownSidType.WorldSid, null);
            var everyoneAccessRule = new PipeAccessRule(everyoneSecurityIdentifier,
                PipeAccessRights.FullControl, AccessControlType.Allow);
            pipeSecurity.AddAccessRule(everyoneAccessRule);

            try
            {
                using (var namedPipeServer = new NamedPipeServerStream(
                    _serverPipeConfiguration.Name, PipeDirection.InOut, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous,
                    0, 0, pipeSecurity))
                {
                    _log.DebugFormat("Start reading pipe: {0}", _serverPipeConfiguration.Name);

                    while (true)
                    {
                        if (_stopReading)
                        {
                            _log.DebugFormat("Stop reading pipe {0} (stop request)", _serverPipeConfiguration.Name);
                            return;
                        }

                        ReadMessage(namedPipeServer);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Stop reading pipe {0} (exception: {1})",
                    _serverPipeConfiguration.Name, ex);
            }
            finally
            {
                _log.DebugFormat("Stop reading pipe {0}", _serverPipeConfiguration.Name);
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

        private static Configuration GetConfiguration()
        {
            var configurationSection = (NamedPipeMessageBusConfigurationSection)
                ConfigurationManager.GetSection("namedPipeMessageBus");

            if (configurationSection == null)
                throw new InvalidOperationException("namedPipeMessageBus configuration sections can't be found");

            return new Configuration(
                configurationSection.Pipes.Cast<NamedPipeElement>(),
                configurationSection.ConnectTimeout);
        }

        private void LoadConfiguration(Configuration configuration)
        {
            _connectTimeout = configuration.ConnectTimeout;

            _serverPipeConfiguration = configuration.Pipes.Single(p => p.IsServer);
            _clientPipesConfiguration = configuration.Pipes.Where(p => !p.IsServer).ToArray();

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

        #region Inner classes

        private class Configuration
        {
            public Configuration(string serverPipeName, string clientPipeName)
            {
                Pipes = new[]
                {
                    new NamedPipeElement() {IsServer = true, Name = serverPipeName},
                    new NamedPipeElement() {IsServer = false, Name = clientPipeName}
                };

                ConnectTimeout = 100;
            }

            public Configuration(IEnumerable<NamedPipeElement> pipes, int connectTimeout)
            {
                Pipes = pipes;
                ConnectTimeout = connectTimeout;
            }

            public IEnumerable<NamedPipeElement> Pipes { get; private set; }

            public int ConnectTimeout { get; private set; }
        }

        #endregion
    }
}