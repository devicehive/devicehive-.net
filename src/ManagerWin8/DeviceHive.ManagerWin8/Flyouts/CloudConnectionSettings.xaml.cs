using DeviceHive.ManagerWin8.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace DeviceHive.ManagerWin8.Flyouts
{
    public sealed partial class CloudConnectionSettings : UserControl
    {
        static CloudConnectionSettings instance;

        CloudConnectionSettings()
        {
            this.InitializeComponent();

            this.DataContext = Settings.Instance;
        }

        public static CloudConnectionSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CloudConnectionSettings();
                }
                return instance;
            }
        }
    }
}
