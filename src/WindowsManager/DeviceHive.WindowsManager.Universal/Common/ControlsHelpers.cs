using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DeviceHive.WindowsManager.Common
{
    public static class ControlsHelpers
    {
        public static void ControlsEnable(this Panel panel, bool isEnabled)
        {
            foreach (UIElement element in panel.Children)
            {
                var el = element as Control;
                if (el != null)
                {
                    el.IsEnabled = isEnabled;
                }
            }
        }
    }
}
