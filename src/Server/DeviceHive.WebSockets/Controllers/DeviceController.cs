using DeviceHive.Core;
using DeviceHive.Core.Mapping;
using DeviceHive.WebSockets.Core;
using DeviceHive.WebSockets.Network;
using DeviceHive.WebSockets.Subscriptions;
using Newtonsoft.Json.Linq;
using Ninject;

namespace DeviceHive.WebSockets.Controllers
{
	public class DeviceController : ControllerBase
	{
		private readonly SubscriptionManager _subscriptionManager;

		public DeviceController(DataContext dataContext, WebSocketServerBase server,
			[Named("DeviceCommand")] SubscriptionManager subscriptionManager,
			JsonMapperManager jsonMapperManager) :
			base(dataContext, server, jsonMapperManager)
		{
			_subscriptionManager = subscriptionManager;
		}

		protected override void InvokeActionImpl()
		{
			throw new System.NotImplementedException();
		}
	}
}