using System;
using System.Configuration;
using System.Linq;
using System.Reflection;

namespace DeviceHive.WebSockets.API
{
    internal sealed class AppConfigLoader : IDisposable
    {
        private const string _appConfigFileKey = "APP_CONFIG_FILE";

        private readonly string _oldConfig =
            AppDomain.CurrentDomain.GetData(_appConfigFileKey).ToString();

        private bool _disposedValue;

        public static AppConfigLoader Load(string path)
        {
            return new AppConfigLoader(path);
        }

        public void Dispose()
        {
            if (!_disposedValue)
            {
                AppDomain.CurrentDomain.SetData(_appConfigFileKey, _oldConfig);
                ResetConfigMechanism();

                _disposedValue = true;
            }
        }        

        public AppConfigLoader(string path)
        {
            AppDomain.CurrentDomain.SetData(_appConfigFileKey, path);
            ResetConfigMechanism();
        }        

        private static void ResetConfigMechanism()
        {
            var configurationManagetType = typeof(ConfigurationManager);

            configurationManagetType
                .GetField("s_initState", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, 0);

            configurationManagetType
                .GetField("s_configSystem", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, null);

            configurationManagetType
                .Assembly.GetTypes()
                .First(x => x.FullName == "System.Configuration.ClientConfigPaths")
                .GetField("s_current", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, null);
        }
    }
}