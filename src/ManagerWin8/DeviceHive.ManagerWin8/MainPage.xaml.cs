using DeviceHive.Client;
using DeviceHive.ManagerWin8.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Grouped Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234231

namespace DeviceHive.ManagerWin8
{
    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class MainPage : DeviceHive.ManagerWin8.Common.LayoutAwarePage
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            LoadDevices();
        }

        async void LoadDevices()
        {
            IsLoading = true;
            try
            {
                VisualStateManager.GoToState(this, "DevicesViewState", true);
                var networkWithDevicesList = new ObservableCollection<NetworkViewModel>();
                DefaultViewModel["Groups"] = networkWithDevicesList;
                (itemSemanticZoomView.ZoomedOutView as ListViewBase).ItemsSource = groupedItemsViewSource.View.CollectionGroups;

                var deviceList = await ClientService.Current.GetDevicesAsync();
                var networkList = (await ClientService.Current.GetNetworksAsync()).FindAll(n => n.Id != null);
                foreach (Network network in networkList)
                {
                    var devices = deviceList.FindAll(d => d.Network.Id == network.Id);
                    if (devices.Count > 0)
                    {
                        networkWithDevicesList.Add(new NetworkViewModel(network) { Devices = devices });
                    }
                }
            }
            catch (Exception ex)
            {
                VisualStateManager.GoToState(this, "EmptySettingsState", true);
                if (!(ex is ClientService.EmptyCloudSettingsException))
                {
                    new MessageDialog(ex.Message + (ex.InnerException != null ? "\n\n" + ex.InnerException.Message : ""), "Error").ShowAsync();
                }
            }
            IsLoading = false;
        }

        private void itemGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var device = e.ClickedItem as Device;
            if (device != null)
            {
                Frame.Navigate(typeof(DevicePage), device.Id);
            }
        }

        private void Refresh_Tapped(object sender, TappedRoutedEventArgs e)
        {
            LoadDevices();
        }

        private void HyperlinkButton_Click_1(object sender, RoutedEventArgs e)
        {
            App.ShowCloudSettings();
        }

        private void HyperlinkButton_Click_2(object sender, RoutedEventArgs e)
        {
            LoadDevices();
        }
    }
}
