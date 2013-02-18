using System;
using System.Threading;
using DeviceHive.Core.Messaging;
using DeviceHive.WebSockets.Core.Network;

namespace DeviceHive.WebSockets.Core.Hosting
{
    public abstract class ApplicationServiceBase : WebSocketServerBase
    {
        private readonly NamedPipeMessageBus _messageBus;
        
        private readonly EventWaitHandle _finishedWaitHandle;

        protected ApplicationServiceBase(string hostPipeName, string appPipeName)
        {
            _messageBus = new NamedPipeMessageBus(appPipeName, hostPipeName);
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