using System;
using System.Threading;
using DeviceHive.Core.Messaging;
using DeviceHive.WebSockets.Core.Network;
using log4net;

namespace DeviceHive.WebSockets.Core.Hosting
{
    public abstract class ApplicationServiceBase : WebSocketServerBase
    {
        private readonly ILog _log;

        private readonly string _hostPipeName;
        private readonly string _appPipeName;

        private NamedPipeMessageBus _messageBus;
        
        private readonly EventWaitHandle _finishedWaitHandle;

        protected ApplicationServiceBase(string hostPipeName, string appPipeName)
        {
            _log = LogManager.GetLogger(GetType());

            _hostPipeName = hostPipeName;
            _appPipeName = appPipeName;
            
            _finishedWaitHandle = new ManualResetEvent(false);
        }


        public override void Start(string url, string sslCertificateSerialNumber)
        {
            throw new NotSupportedException("Use Start without parameters");
        }

        public void Start()
        {
            _finishedWaitHandle.Reset();
            Init();

            _messageBus = new NamedPipeMessageBus(_appPipeName, _hostPipeName);

            _messageBus.Subscribe((ConnectionOpenedMessage msg) =>
            {
                RegisterConnection(new ProxyWebSocketConnection(msg, this));
            });

            _messageBus.Subscribe((ConnectionClosedMessage msg) =>
            {
                UnregisterConnection(msg.ConnectionIdentity);
            });

            _messageBus.Subscribe((DataReceivedMessage msg) =>
            {
                var c = GetConnection(msg.ConnectionIdentity);
                if (c == null)
                {
                    _log.Error("Received message for not existing connection");
                    return;
                }

                OnMessageReceived(new WebSocketMessageEventArgs(c, msg.Data));
            });

            _messageBus.Subscribe((CloseApplicationMessage msg) =>
            {
                Stop();
            });

            _messageBus.Notify(new ApplicationActivatedMessage());
        }

        public override void Stop()
        {
            _messageBus.Dispose();
            _finishedWaitHandle.Set();
        }

        public EventWaitHandle FinishedWaitHandle
        {
            get { return _finishedWaitHandle; }
        }


        protected virtual void Init()
        {            
        }


        internal void SendMessage(Guid connectionIdentity, string data)
        {
            _messageBus.Notify(new SendDataMessage(connectionIdentity, data));
        }

        internal void CloseConnection(Guid connectionIdentity)
        {
            _messageBus.Notify(new CloseConnectionMessage(connectionIdentity));
        }
    }
}