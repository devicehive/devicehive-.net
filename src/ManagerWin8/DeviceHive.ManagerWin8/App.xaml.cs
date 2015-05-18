using DeviceHive.ManagerWin8.Common;
using DeviceHive.ManagerWin8.Flyouts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ApplicationSettings;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Split App template is documented at http://go.microsoft.com/fwlink/?LinkId=234228

namespace DeviceHive.ManagerWin8
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton Application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                //Associate the frame with a SuspensionManager key                                
                SuspensionManager.RegisterFrame(rootFrame, "AppFrame");

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // Restore the saved session state only when appropriate
                    try
                    {
                        await SuspensionManager.RestoreAsync();
                    }
                    catch (SuspensionManagerException)
                    {
                        //Something went wrong restoring state.
                        //Assume there is no state and continue
                    }
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }
            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(typeof(MainPage)))
                {
                    throw new Exception("Failed to create initial page");
                }
            }

            SettingsPane.GetForCurrentView().CommandsRequested += App_CommandsRequested;

            // Ensure the current window is active
            Window.Current.Activate();

            //Only when the debugger is attached
            if (System.Diagnostics.Debugger.IsAttached)
            {
                //Display the metro grid helper
                //MC.MetroGridHelper.MetroGridHelper.CreateGrid();
            }
        }

        void App_CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            args.Request.ApplicationCommands.Clear();

            args.Request.ApplicationCommands.Add(new SettingsCommand("CloudConnection", "Cloud connection", (x) => ShowCloudSettings()));
            args.Request.ApplicationCommands.Add(new SettingsCommand("OpenSite", "DeviceHive.com", (x) =>
            {
                Windows.System.Launcher.LaunchUriAsync(new Uri("http://devicehive.com"));
            }));
            args.Request.ApplicationCommands.Add(new SettingsCommand("PrivacyPolicy", "Privacy Policy", (x) =>
            {
                Windows.System.Launcher.LaunchUriAsync(new Uri("http://apps.dataart.com/PrivacyPolicy.htm"));
            }));
        }

        public static void ShowCloudSettings()
        {
            SettingsFlyout settings = new SettingsFlyout();
            settings.Title = "Cloud connection";
            settings.HeaderBackground = new SolidColorBrush(Color.FromArgb(255, 26, 160, 255));
            settings.HorizontalContentAlignment = HorizontalAlignment.Stretch;

            settings.Content = CloudConnectionSettings.Instance;
            settings.Show();
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }
    }
}
