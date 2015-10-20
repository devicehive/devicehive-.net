using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace DeviceHive.WindowsManager
{
    public class Settings : INotifyPropertyChanged
    {
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
                return GetValueOrDefault<string>(() => "http://pg.devicehive.com/api");
            }
            set
            {
                AddOrUpdateValue(value);
            }
        }

        public string CloudUsername
        {
            get
            {
                return GetValueOrDefault<string>(() => "admin");
            }
            set
            {
                AddOrUpdateValue(value);
            }
        }

        public string CloudPassword
        {
            get
            {
                return GetValueOrDefault<string>();
            }
            set
            {
                AddOrUpdateValue(value);
            }
        }

        public string CloudAccessKey
        {
            get
            {
                return GetValueOrDefault<string>();
            }
            set
            {
                AddOrUpdateValue(value);
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
        bool AddOrUpdateValue(Object value, [CallerMemberName]string Key = "")
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
            if (valueChanged)
            {
                OnPropertyChanged(Key);
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
        T GetValueOrDefault<T>(Func<T> defaultValueCreator, [CallerMemberName]string Key = "")
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
                if (defaultValueCreator == null)
                {
                    value = default(T);
                }
                else
                {
                    value = defaultValueCreator();
                }
                settings.Values.Add(Key, value);
            }
            return value;
        }

        T GetValueOrDefault<T>([CallerMemberName]string Key = "")
        {
            return GetValueOrDefault<T>(null, Key);
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            var tempEvent = PropertyChanged;
            if (tempEvent != null)
            {
                tempEvent(this, e);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
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
