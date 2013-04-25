using System;
using System.Collections.Generic;

namespace DeviceHive.WebSockets.Core.Network
{
    public class SessionContext
    {
        private readonly object _lock = new object();
        private readonly IDictionary<string, object> _data = new Dictionary<string, object>();

        public void Clear()
        {
            lock (_lock)
                _data.Clear();
        }

        public object this[string key]
        {
            get
            {
                object value;
                lock (_lock)
                    return _data.TryGetValue(key, out value) ? value : null;
            }
            set
            {
                lock (_lock)
                    _data[key] = value;
            }
        }

        public object GetOrAdd(string key, Func<object> objCreator)
        {
            lock (_lock)
            {
                object val;
                if (!_data.TryGetValue(key, out val))
                {
                    val = objCreator();
                    _data.Add(key, val);
                }

                return val;
            }
        }
    }
}