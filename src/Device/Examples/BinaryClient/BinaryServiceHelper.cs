using System;
using System.Collections.Generic;
using DeviceHive.Device;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace DeviceHive.Binary
{
    public class BinaryServiceHelper : BinaryServiceBase
    {
        private Dictionary<int, TaskCompletionSource<CommandResult>> _cmdData;
        private const int _timeout = 10000;
        private int _cmdId;
        private TaskCompletionSource<DeviceRegistrationInfo> _regComplete;
        
        public struct CommandResult
        {
            public string Status;
            public string Result;
            public long ExecutionTime;
        };

        public BinaryServiceHelper(IBinaryConnection connection)
            : base(connection)
        {
            _cmdId = 0;
            _cmdData = new Dictionary<int, TaskCompletionSource<CommandResult>>();
        }

        public override void Start()
        {
            _regComplete = new TaskCompletionSource<DeviceRegistrationInfo>();
            base.Start();
            RequestRegistration();
            _regComplete.Task.Wait();
        }

        public async Task<CommandResult> SendCmd(string command, object arg)
        {
            int cid = Interlocked.Increment(ref _cmdId);

            Command cmd = new Command()
            {
                Id = cid,
                Name = command
            };
            cmd.Parameters = Newtonsoft.Json.Linq.JToken.FromObject(arg);
            Stopwatch sw = new Stopwatch();

            TaskCompletionSource<CommandResult> tks = new TaskCompletionSource<CommandResult>();
            _cmdData.Add(cid, tks);

            sw.Start();

            base.SendCommand(cmd);

            if (await Task.WhenAny(tks.Task, Task.Delay(_timeout)) != tks.Task)
            {
                throw new Exception("Timeout");
            }

            CommandResult rv = await tks.Task;
            sw.Stop();        
            
            _cmdData.Remove(cid);
            rv.ExecutionTime = sw.ElapsedMilliseconds;
            return rv;
        }

        protected override void HandleHotification(DeviceHive.Device.Notification notification)
        {
            throw new NotImplementedException();
        }

        protected override void NotifyCommandResult(int commandId, string status, string result)
        {
            try
            {
                TaskCompletionSource<CommandResult> cr = _cmdData[commandId];
                CommandResult rv = new CommandResult()
                {
                    Result = result,
                    Status = status
                };

                cr.SetResult(rv);
            }
            catch (Exception ) { } // if a command with unknown id comes in - we aren't interested in it
        }

        protected override void RegisterDevice(DeviceRegistrationInfo registrationInfo)
        {
            _regComplete.SetResult(registrationInfo);
        }
    }
 }
