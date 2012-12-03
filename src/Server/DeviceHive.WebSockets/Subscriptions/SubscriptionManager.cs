using System;
using System.Collections.Generic;
using System.Linq;
using DeviceHive.WebSockets.Network;

namespace DeviceHive.WebSockets.Subscriptions
{
	public class SubscriptionManager
	{
		private readonly SubscriptionCollection _subscriptionCollection = new SubscriptionCollection();

	 	public void Subscribe(WebSocketConnectionBase connection, Guid? deviceGuid)
	 	{
			var subscription = new Subscription(deviceGuid, connection);	 		
	 		
			var connectionSubscriptions = GetSubscriptions(connection);
	 		connectionSubscriptions.Add(subscription);

			var subscriptionList = _subscriptionCollection.GetSubscriptionList(deviceGuid);
			subscriptionList.Add(subscription);
	 	}

		public void Unsubscribe(WebSocketConnectionBase connection, Guid? deviceGuid)
		{
			var connectionSubscriptions = GetSubscriptions(connection);
			connectionSubscriptions.RemoveAll(s => s.DeviceGuid == deviceGuid);

			var subscriptionList = _subscriptionCollection.GetSubscriptionList(deviceGuid);
			subscriptionList.RemoveAll(s => s.Connection == connection);
		}

		public IEnumerable<WebSocketConnectionBase> GetConnections(Guid? deviceGuid)
		{
			return _subscriptionCollection.GetSubscriptionList(deviceGuid)
				.Concat(_subscriptionCollection.GetSubscriptionList(null))
				.Select(s => s.Connection)
				.Distinct()
				.ToArray();
		}

		public void Cleanup(WebSocketConnectionBase connection)
		{
			var deviceGuids = GetSubscriptions(connection).Select(s => s.DeviceGuid).Distinct().ToArray();

			foreach (var deviceGuid in deviceGuids)
			{
				var subscriptionList = _subscriptionCollection.GetSubscriptionList(deviceGuid);
				subscriptionList.RemoveAll(s => s.Connection == connection);
			}
		}


		private List<Subscription> GetSubscriptions(WebSocketConnectionBase connection)
		{
			return (List<Subscription>) connection.Session.GetOrAdd(
				"Subscriptions", () => new List<Subscription>());
		}


		#region Inner classes

		private class SubscriptionCollection
		{
			private readonly object _lock = new object();

			private readonly Dictionary<Guid, SubscriptionList> _subscriptions =
				new Dictionary<Guid, SubscriptionList>();

			public SubscriptionList GetSubscriptionList(Guid? deviceGuid)
			{
				var key = deviceGuid ?? Guid.Empty;

				lock (_lock)
				{
					SubscriptionList list;
					if (!_subscriptions.TryGetValue(key, out list))
					{
						list = new SubscriptionList();
						_subscriptions.Add(key, list);
					}

					return list;
				}
			}
		}

		private class SubscriptionList : List<Subscription>
		{			
		}

		#endregion
	}
}