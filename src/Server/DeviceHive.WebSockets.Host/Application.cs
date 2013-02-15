namespace DeviceHive.WebSockets.Host
{
    internal class Application
    {
        private readonly string _host;
        private readonly string _exePath;

        public Application(string host, string exePath)
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

        public ApplicationState State { get; private set; }



    }    

    public enum ApplicationState
    {
        Stopped,
        Stopping,
        Inactive,
        Activating,
        Active
    }
}