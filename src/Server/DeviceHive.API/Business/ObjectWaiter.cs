using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DeviceHive.API.Business
{
    public class ObjectWaiter<T>
    {
        private Dictionary<object, ManualResetEvent> _handles = new Dictionary<object, ManualResetEvent>();

        #region Public Methods

        public List<T> WaitForObjects(object key, Func<List<T>> getter, TimeSpan timeout)
        {
            var timestamp = DateTime.UtcNow;
            var handle = GetHandle(key);
            while (true)
            {
                lock (handle)
                {
                    var result = getter();
                    if (result != null && result.Any())
                        return result;

                    handle.Reset();
                }

                if (!handle.WaitOne((timestamp + timeout) - DateTime.UtcNow))
                    return new List<T>();
            }
        }

        public void NotifyChanges(object key)
        {
            var handle = GetHandle(key);
            lock (handle)
            {
                handle.Set();
            }
        }
        #endregion

        #region Private Methods

        private ManualResetEvent GetHandle(object key)
        {
            lock (_handles)
            {
                ManualResetEvent handle;
                if (!_handles.TryGetValue(key, out handle))
                {
                    handle = new ManualResetEvent(false);
                    _handles[key] = handle;
                }
                return handle;
            }
        }
        #endregion
    }
}