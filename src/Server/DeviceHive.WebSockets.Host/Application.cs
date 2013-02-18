using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using DeviceHive.Core.Messaging;
using DeviceHive.WebSockets.Core.Hosting;
using DeviceHive.WebSockets.Core.Network;
using log4net;

namespace DeviceHive.WebSockets.Host
{
    internal class Application
    {
        private readonly object _lock = new object();

        private readonly ILog _log;

        private readonly string _host;
        private readonly string _exePath;
        private readonly string _commandLineArgs;

        private readonly WebSocketServerBase _server;

        private readonly string _hostPipeName;
        private readonly string _appPipeName;
        private readonly int _terminateTimeout;

        private ApplicationState _state;
        
        private NamedPipeMessageBus _messageBus;
        private Process _process;

        private readonly Queue<MessageContainer> _messageQueue = new Queue<MessageContainer>();


        public Application(WebSocketServerBase server,
            ServiceConfigurationSection configuration,
            string host, string exePath, string commandLineArgs)
        {
            _log = LogManager.GetLogger(GetType());

            _server = server;

            _hostPipeName = string.Format(configuration.HostPipeName, host);
            _appPipeName = string.Format(configuration.AppPipeName, host);
            _terminateTimeout = configuration.ApplicationTerminateTimeout;

            _host = host;
            _exePath = exePath;
            _commandLineArgs = commandLineArgs;

            _state = ApplicationState.Inactive;
        }


        public string Host
        {
            get { return _host; }
        }

        public string ExePath
        {
            get { return _exePath; }
        }

        public string CommandLineArgs
        {
            get { return _commandLineArgs; }
        }

        public ApplicationState State
        {
            get { return _state; }
        }


        public void Stop()
        {
            lock (_lock)
            {
                Shutdown();
                _state = ApplicationState.Stopped;                
            }
        }

        public void NotifyConnectionOpened(WebSocketConnectionBase connection)
        {
            SendMessage(new ConnectionOpenedMessage(connection));
        }

        public void NotifyConnectionClosed(WebSocketConnectionBase connection)
        {
            SendMessage(new ConnectionClosedMessage(connection.Identity));
        }

        public void NotifyMessageReceived(WebSocketConnectionBase connection, string message)
        {
            SendMessage(new DataReceivedMessage(connection.Identity, message));
        }


        private void SendMessage<TMessage>(TMessage message) where TMessage : class
        {
            lock (_lock)
            {
                if ( _state == ApplicationState.Stopped)
                {
                    _log.ErrorFormat("Attempt to send message to stopped app: {0}", _host);
                    return;
                }

                if (_state == ApplicationState.Active)
                {
                    _messageBus.Notify(message);
                    return;
                }

                _messageQueue.Enqueue(MessageContainer.Create(message));

                if (_state == ApplicationState.Inactive)
                {
                    _state = ApplicationState.Activating;
                    ThreadPool.QueueUserWorkItem(s => Activate());
                }
            }
        }

        private void Activate()
        {
            try
            {
                InitializeMessageBus();
                StartProcess();
            }
            catch (Exception e)
            {
                _log.ErrorFormat("Can't start application for host: {0}. Error: {1}", _host, e);

                lock (_lock)
                {
                    if (_messageBus != null)
                    {
                        _messageBus.Dispose();
                        _messageBus = null;
                    }

                    _messageBus = null;
                    _process = null;
                    _state = ApplicationState.Inactive;
                }
            }
        }       

        private void InitializeMessageBus()
        {
            _messageBus = new NamedPipeMessageBus(_hostPipeName, _appPipeName);            
            _messageBus.Subscribe((ApplicationActivatedMessage msg) => OnApplicationActivated());            
            _messageBus.Subscribe((SendDataMessage msg) => SendMessage(msg.ConnectionIdentity, msg.Data));
            _messageBus.Subscribe((CloseConnectionMessage msg) => CloseConnection(msg.ConnectionIdentity));
        }

        private void StartProcess()
        {
            _process = new Process();
            _process.StartInfo = new ProcessStartInfo(ExePath, CommandLineArgs) { UseShellExecute = false };
            _process.Start();
        }

        public void Shutdown()
        {
            if (_process != null)
            {
                _messageBus.Notify(new CloseApplicationMessage());
                if (!_process.WaitForExit(_terminateTimeout))
                    _process.Kill();

                _process = null;
            }

            if (_messageBus != null)
            {
                _messageBus.Dispose();
                _messageBus = null;
            }
        }

        private void OnApplicationActivated()
        {
            lock (_lock)
            {
                _state = ApplicationState.Active;

                foreach (var messageContainer in _messageQueue)
                    _messageBus.Notify(messageContainer.Message, messageContainer.Type);
            }
        }

        private void SendMessage(Guid connectionIdentity, string data)
        {
            lock (_lock)
            {
                if (_state != ApplicationState.Active)
                    return;

                var conn = _server.GetConnection(connectionIdentity);
                if (conn != null)
                    conn.Send(data);
            }
        }

        private void CloseConnection(Guid connectionIdentity)
        {
            lock (_lock)
            {
                if (_state != ApplicationState.Active)
                    return;

                var conn = _server.GetConnection(connectionIdentity);
                if (conn != null)
                    conn.Close();
            }
        }


        private class MessageContainer
        {
            private readonly Type _type;
            private readonly object _message;

            public MessageContainer(Type type, object message)
            {
                _type = type;
                _message = message;
            }

            public Type Type
            {
                get { return _type; }
            }

            public object Message
            {
                get { return _message; }
            }

            public static MessageContainer Create<TMessage>(TMessage message)
            {
                return new MessageContainer(typeof (TMessage), message);
            }
        }
    }

    public enum ApplicationState
    {
        Stopped,
        Inactive,
        Activating,
        Active,
    }
}