using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace DeviceHive.ManagerWin8
{
    public class Settings : INotifyPropertyChanged
    {
        const string cloudServerUrlKey = "CloudServerUrl";
        const string cloudUsernameKey = "CloudUsername";
        const string cloudPasswordKey = "CloudPassword";

        static Settings instance;
        ApplicationDataContainer settings;

        public event PropertyChangedEventHandler PropertyChanged;

        Settings()
        {
            settings = ApplicationData.Current.RoamingSettings;
        }

        public string CloudServerUrl
        {
            get
            {
                return GetValueOrDefault<string>(cloudServerUrlKey, "http://pg.devicehive.com/api");
            }
            set
            {
                if (AddOrUpdateValue(cloudServerUrlKey, value))
                {
                    OnPropertyChanged("CloudServerUrl");
                }
            }
        }

        public string CloudUsername
        {
            get
            {
                return GetValueOrDefault<string>(cloudUsernameKey, "admin");
            }
            set
            {
                if (AddOrUpdateValue(cloudUsernameKey, value))
                {
                    OnPropertyChanged("CloudUsername");
                }
            }
        }

        public string CloudPassword
        {
            get
            {
                return GetValueOrDefault<string>(cloudPasswordKey);
            }
            set
            {
                if (AddOrUpdateValue(cloudPasswordKey, value))
                {
                    OnPropertyChanged("CloudPassword");
                }
            }
        }

        #region Settings utils

        /// <summary>
        /// Update a setting value for our application. If the setting does not
        /// exist, then add the setting.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool AddOrUpdateValue(string Key, Object value)
        {
            bool valueChanged = false;

            // If the key exists
            if (settings.Values.ContainsKey(Key))
            {
                // If the value has changed
                if (settings.Values[Key] != value)
                {
                    // Store the new value
                    settings.Values[Key] = value;
                    valueChanged = true;
                }
            }
            // Otherwise create the key.
            else
            {
                settings.Values.Add(Key, value);
                valueChanged = true;
            }
            return valueChanged;
        }

        /// <summary>
        /// Get the current value of the setting, or if it is not found, set the 
        /// setting to the default setting.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        T GetValueOrDefault<T>(string Key, T defaultValue)
        {
            T value;

            // If the key exists, retrieve the value.
            if (settings.Values.ContainsKey(Key))
            {
                value = (T)settings.Values[Key];
            }
            // Otherwise, use the default value.
            else
            {
                value = defaultValue;
            }
            return value;
        }

        T GetValueOrDefault<T>(string Key)
        {
            return GetValueOrDefault<T>(Key, default(T));
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            var tempEvent = PropertyChanged;
            if (tempEvent != null)
            {
                tempEvent(this, e);
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var tempEvent = PropertyChanged;
            if (tempEvent != null)
            {
                tempEvent(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public static Settings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Settings();
                }
                return instance;
            }
        }

        #endregion
    }
}
