using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Threading;
using DeviceHive.WebSockets.Core.Hosting;
using DeviceHive.WebSockets.Core.Network;

namespace DeviceHive.WebSockets.Host
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    internal class HostServiceImpl : IWebSocketApplicationManager
    {
        private readonly ApplicationCollection _applications = new ApplicationCollection();
        private readonly WebSocketServerBase _server;
        
        private ServiceConfigurationSection _configSection;
        private RuntimeServiceConfiguration _runtimeConfig;

        private Timer _inactiveAppCheckTimer;

        private readonly ServiceHost _managerServiceHost;

        
        public HostServiceImpl(WebSocketServerBase server)
        {
            _server = server;
            _server.ConnectionOpened += OnConnectionOpened;
            _server.MessageReceived += OnMessageReceived;
            _server.ConnectionClosed += OnConnectionClosed;            
            _managerServiceHost = new ServiceHost(this);
        }


        #region Public methods

        public void Start()
        {
            var appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Directory.SetCurrentDirectory(appDir);

            _configSection = (ServiceConfigurationSection)ConfigurationManager.GetSection("webSocketsHost");
            _runtimeConfig = RuntimeServiceConfiguration.Load(_configSection.RuntimeConfigPath);

            LoadApplications();

            var url = _configSection.ListenUrl;
            var sslCertificateSerialNumber = _configSection.CertificateSerialNumber;

            _server.Start(url, sslCertificateSerialNumber);

            _inactiveAppCheckTimer = new Timer(state => CheckInactiveApplications());
            var applicationInactiveCheckInterval = _configSection.ApplicationInactiveCheckInterval * 60 * 1000;
            _inactiveAppCheckTimer.Change(applicationInactiveCheckInterval, applicationInactiveCheckInterval);

            _managerServiceHost.Open();
        }

        public void Stop()
        {
            _managerServiceHost.Close();

            _inactiveAppCheckTimer.Dispose();

            _server.Stop();

            foreach (var app in _applications.GetAllApplications())
                app.Stop();            
        }


        #region Implementation of IWebSocketApplicationManager

        public void AddApplication(string host, string exePath, string commandLineArgs,
            string userName, string userPassword)
        {
            var appConfig = new ApplicationConfiguration()
            {
                Host = host,
                ExePath = exePath,
                CommandLineArgs = commandLineArgs,
                UserName = userName,
                UserPassword = userPassword
            };

            AddApplication(appConfig);

            _runtimeConfig.Applications.Add(appConfig);
            _runtimeConfig.Save();
        }

        public bool RemoveApplication(string host)
        {
            if (!_applications.Remove(host))
                return false;

            var appConfig = _runtimeConfig.Applications.FirstOrDefault(c => c.Host.ToLower() == host.ToLower());
            _runtimeConfig.Applications.Remove(appConfig);
            _runtimeConfig.Save();
            return true;
        }

        public bool ChangeApplication(string host,
            string exePath = null, string commandLineArgs = null,
            string userName = null, string userPassword = null)
        {
            var app = _applications.GetApplicationByHost(host);
            if (app == null || app.State != ApplicationState.Stopped)
                return false;

            var appConfig = _runtimeConfig.Applications.FirstOrDefault(c => c.Host.ToLower() == host.ToLower());
            appConfig.ExePath = exePath ?? appConfig.ExePath;
            appConfig.CommandLineArgs = commandLineArgs ?? appConfig.CommandLineArgs;
            appConfig.UserName = userName ?? appConfig.UserName;
            appConfig.UserPassword = userPassword ?? appConfig.UserPassword;
            _runtimeConfig.Save();

            _applications.Remove(host);
            app = new Application(_server, _configSection, appConfig);
            _applications.Add(app);

            return true;
        }

        public bool StopApplication(string host)
        {
            var app = _applications.GetApplicationByHost(host);
            if (app == null)
                return false;

            app.Stop();
            return true;
        }

        public bool StartApplication(string host)
        {
            var app = _applications.GetApplicationByHost(host);
            if (app == null)
                return false;

            app.Start();
            return true;
        }

        #endregion

        #endregion


        #region Private methods

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
            var app = new Application(_server, _configSection, appConfig);
            _applications.Add(app);
        }


        private void CheckInactiveApplications()
        {
            var apps = _applications.GetAllApplications();
            var minAccessTime = DateTime.Now.AddMinutes(-_configSection.ApplicationInactiveTimeout);

            foreach (var app in apps)
                app.TryDeactivate(minAccessTime);
        }

        #endregion
    }
}