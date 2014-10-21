using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceHive.Client
{
    internal class CommandCallback
    {
        #region Public Properties
        
        public Action<Command> Callback { get; private set; }
        public TaskCompletionSource<object> WaitHandle { get; private set; }

        #endregion

        #region Constructor

        public CommandCallback()
        {
            WaitHandle = new TaskCompletionSource<object>();
        }

        public CommandCallback(Action<Command> callback)
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            Callback = callback;
        }
        #endregion

        #region Public Methods

        public void SetCallback(Action<Command> callback)
        {
            if (callback == null)
                throw new ArgumentNullException("callback");
            if (Callback != null)
                throw new InvalidOperationException("Callback has already been set!");

            Callback = callback;
            WaitHandle.SetResult(true);
        }
        #endregion
    }
}
