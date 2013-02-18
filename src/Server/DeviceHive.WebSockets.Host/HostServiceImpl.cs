using System.Configuration;
using DeviceHive.WebSockets.Core.Network;

namespace DeviceHive.WebSockets.Host
{
    internal class HostServiceImpl
    {
        private readonly ApplicationCollection _applications = new ApplicationCollection();
        private readonly WebSocketServerBase _server;
        
        private readonly ServiceConfigurationSection _configSection;
        private readonly RuntimeServiceConfiguration _runtimeConfig;

        
        public HostServiceImpl(WebSocketServerBase server)
        {
            _server = server;
            _server.ConnectionOpened += OnConnectionOpened;
            _server.MessageReceived += OnMessageReceived;
            _server.ConnectionClosed += OnConnectionClosed;

            _configSection = (ServiceConfigurationSection) ConfigurationManager.GetSection("webSocketsHost");
            _runtimeConfig = RuntimeServiceConfiguration.Load(_configSection.RuntimeConfigPath);

            LoadApplications();
        }
   

        public void Start()
        {
            var url = _configSection.ListenUrl;
            var sslCertificateSerialNumber = _configSection.CertificateSerialNumber;

            _server.Start(url, sslCertificateSerialNumber);
        }

        public void Stop()
        {
            _server.Stop();

            foreach (var app in _applications.GetAllApplications())
                app.Stop();
        }


        private void OnConnectionOpened(object sender, WebSocketConnectionEventArgs args)
        {
            var app = _applications.GetApplicationByHost(args.Connection.Host);
            if (app != null)
                app.NotifyConnectionOpened(args.Connection);
        }

        private void OnMessageReceived(object sender, WebSocketMessageEventArgs args)
        {
            var app = _applications.GetApplicationByHost(args.Connection.Host);
            if (app != null)
                app.NotifyMessageReceived(args.Connection, args.Message);
        }

        private void OnConnectionClosed(object sender, WebSocketConnectionEventArgs args)
        {
            var app = _applications.GetApplicationByHost(args.Connection.Host);
            if (app != null)
                app.NotifyConnectionClosed(args.Connection);
        }


        private void LoadApplications()
        {
            foreach (var appConfig in _runtimeConfig.Applications)
                AddApplication(appConfig);
        }

        private void AddApplication(ApplicationConfiguration appConfig)
        {
            var app = new Application(_server,
                string.Format(_configSection.HostPipeName, appConfig.Host),
                string.Format(_configSection.AppPipeName, appConfig.Host),
                appConfig.Host, appConfig.ExePath, appConfig.CommandLineArgs);
            _applications.Add(app);
        }
    }
}