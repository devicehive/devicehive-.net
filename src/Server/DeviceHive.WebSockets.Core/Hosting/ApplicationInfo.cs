namespace DeviceHive.WebSockets.Core.Hosting
{
    public class ApplicationInfo
    {
        private readonly string _host;
        private readonly string _exePath;

        public ApplicationInfo(string host, string exePath)
        {
            _host = host;
            _exePath = exePath;

            State = ApplicationState.Inactive;
        }

        public string Host
        {
            get { return _host; }
        }

        public string ExePath
        {
            get { return _exePath; }
        }

        public ApplicationState State { get; set; }
    }

    public enum ApplicationState
    {
        Stopped,
        Inactive,
        Active
    }
}