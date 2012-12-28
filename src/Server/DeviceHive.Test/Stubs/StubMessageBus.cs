using DeviceHive.Core.Messaging;

namespace DeviceHive.Test.Stubs
{
    public class StubMessageBus : MessageBus
    {
        protected override void SendMessage(byte[] data)
        {
            HandleMessage(data);
        }
    }
}