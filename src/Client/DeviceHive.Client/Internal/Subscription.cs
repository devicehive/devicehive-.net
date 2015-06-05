using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceHive.Client
{
    internal class Subscription : ISubscription
    {
        #region Public Properties

        public Guid Id { get; set; }
        public SubscriptionType Type { get; private set; }
        public string[] DeviceGuids { get; private set; }
        public string[] EventNames { get; private set; }
        public Action<object> Callback { get; private set; }
        public DateTime Timestamp { get; set; }

        #endregion

        #region Constructor

        public Subscription(SubscriptionType type, string[] deviceGuids, string[] eventNames, Action<object> callback, DateTime timestamp)
        {
            Type = type;
            DeviceGuids = deviceGuids;
            EventNames = eventNames;
            Callback = callback;
            Timestamp = timestamp;
        }
        #endregion
    }
}
