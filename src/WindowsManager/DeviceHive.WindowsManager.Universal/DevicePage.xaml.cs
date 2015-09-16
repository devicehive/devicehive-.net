using DeviceHive.Client;
using DeviceHive.WindowsManager.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Group Detail Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234229

namespace DeviceHive.WindowsManager
{
    /// <summary>
    /// A page that displays an overview of a single group, including a preview of the items
    /// within the group.
    /// </summary>
    public sealed partial class DevicePage : DeviceHive.WindowsManager.Common.LayoutAwarePage
    {
        string deviceId;
        Device device;

        List<Tab> tabList = new List<Tab>();
        Tab currentTab;

        bool equipmentInited;
        bool wasSubscriptionError;
        IncrementalLoadingCollection<Notification> notificationsObservable;
        IncrementalLoadingCollection<Command> commandsObservable;
        ISubscription commandsSubscription;
        ISubscription notificationsSubscription;
        DateTime? filterNotificationsStart;
        DateTime? filterNotificationsEnd;
        DateTime? filterCommandsStart;
        DateTime? filterCommandsEnd;
        Command commandSelected;

        public DevicePage()
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
        protected override async void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            Device = JsonConvert.DeserializeObject<Device>((string)navigationParameter);

            filterNotificationsStart = DateTime.Now.AddDays(-7);
            filterCommandsStart = DateTime.Now.AddDays(-7);

            await LoadDevice();

            if (Device == null)
            {
                return;
            }

            tabList.Add(new Tab("Summary", "SummaryState", true)
            {
                Refresh = () =>
                {
                    LoadDevice();
                }
            });
            tabList.Add(new Tab("Notifications", "NotificationsState")
            {
                Select = () =>
                {
                    if (NotificationsObservable == null)
                    {
                        LoadNotifications();
                    }
                },
                Deselect = () => { },
                Refresh = RefreshNotifications,
                Filter = (sender) =>
                {
                    ShowFilterFlyout(sender, filterNotificationsStart, filterNotificationsEnd, (s, e) =>
                    {
                        filterNotificationsStart = s;
                        filterNotificationsEnd = e;
                        RefreshNotifications();
                    });
                }
            });
            tabList.Add(new Tab("Commands", "CommandsState")
            {
                Select = () =>
                {
                    if (CommandsObservable == null)
                    {
                        LoadCommands();
                    }
                },
                Deselect = () => { },
                Refresh = RefreshCommands,
                Filter = (sender) =>
                {
                    ShowFilterFlyout(sender, filterCommandsStart, filterCommandsEnd, (s, e) =>
                    {
                        filterCommandsStart = s;
                        filterCommandsEnd = e;
                        RefreshCommands();
                    });
                }
            });
            tabList.Add(new Tab("Equipment", "EquipmentState")
            {
                Select = () =>
                {
                    if (!equipmentInited)
                    {
                        equipmentInited = true;
                        LoadEquipment();
                    }
                },
                Refresh = () =>
                {
                    if (!IsLoading)
                    {
                        LoadEquipment();
                    }
                }
            });
            DefaultViewModel["Tabs"] = tabList;

            CommandSelected = null;
        }

        protected override void SaveState(Dictionary<string, object> pageState)
        {
            StopCommandsSubscription();
            StopNotificationsSubscription();
        }

        void ShowFilterFlyout(object sender, DateTime? start, DateTime? end, Action<DateTime?, DateTime?> filterAction)
        {
            Flyout flyOut = new Flyout();
            flyOut.Placement = FlyoutPlacementMode.Top;

            flyOut.FlyoutPresenterStyle = new Style(typeof(FlyoutPresenter));
            flyOut.FlyoutPresenterStyle.Setters.Add(new Setter(FlyoutPresenter.RequestedThemeProperty, ElementTheme.Dark));
            flyOut.FlyoutPresenterStyle.Setters.Add(new Setter(FlyoutPresenter.PaddingProperty, 10));

            StackPanel filterPanel = new StackPanel();
            filterPanel.Margin = new Thickness(10);
            filterPanel.Orientation = Orientation.Vertical;
            filterPanel.Children.Add(new TextBlock() { Text = "Start time", FontSize = 14.8 });
            TextBox filterStart = new TextBox();
            filterPanel.Children.Add(filterStart);
            filterPanel.Children.Add(new TextBlock() { Text = "End time", FontSize = 14.8, Margin = new Thickness(0, 10, 0, 0) });
            TextBox filterEnd = new TextBox();
            filterPanel.Children.Add(filterEnd);
            filterPanel.Children.Add(new TextBlock() { Text = "Leave field empty to subscribe to new data from server" });
            Button filterDoButton = new Button() { Content = "Filter", Margin = new Thickness(0, 10, 0, 0) };
            filterPanel.Children.Add(filterDoButton);

            filterStart.Text = start != null ? start.ToString() : "";
            filterEnd.Text = end != null ? end.ToString() : "";
            filterDoButton.Command = new DelegateCommand(() =>
            {
                start = null;
                end = null;
                DateTime newStart, newEnd;
                if (filterStart.Text != "")
                {
                    if (DateTime.TryParse(filterStart.Text, out newStart))
                    {
                        start = newStart;
                    }
                    else
                    {
                        new MessageDialog("Wrong start date", "Filter").ShowAsync();
                        return;
                    }
                }
                if (filterEnd.Text != "")
                {
                    if (DateTime.TryParse(filterEnd.Text, out newEnd))
                    {
                        end = newEnd;
                    }
                    else
                    {
                        new MessageDialog("Wrong end date", "Filter").ShowAsync();
                        return;
                    }
                }
                filterAction(start, end);
            });

            flyOut.Content = filterPanel;
            flyOut.ShowAt((FrameworkElement)sender);
             
        }

        public Device Device
        {
            protected set
            {
                device = value;
                DefaultViewModel["Device"] = device;
                if (device != null)
                {
                    deviceId = device.Id;
                }
            }
            get { return device; }
        }

        public IncrementalLoadingCollection<Notification> NotificationsObservable
        {
            get { return notificationsObservable; }
            set
            {
                notificationsObservable = value;
                DefaultViewModel["Notifications"] = notificationsObservable;
            }
        }

        public IncrementalLoadingCollection<Command> CommandsObservable
        {
            get { return commandsObservable; }
            set
            {
                commandsObservable = value;
                DefaultViewModel["Commands"] = commandsObservable;
            }
        }


        public Command CommandSelected
        {
            get { return commandSelected; }
            set
            {
                commandSelected = value;
                DefaultViewModel["IsCommandSelected"] = commandSelected != null;
                BottomAppBar.IsOpen = commandSelected != null;
            }
        }

        void RefreshNotifications()
        {
            if (!IsLoading)
            {
                LoadNotifications();
            }
        }

        void RefreshCommands()
        {
            if (!IsLoading)
            {
                LoadCommands();
            }
        }

        async Task StopNotificationsSubscription()
        {
            if (notificationsSubscription != null)
            {
                try
                {
                    Debug.WriteLine("NTF subscription CANCEL");
                    await ClientService.Current.RemoveSubscriptionAsync(notificationsSubscription);
                    notificationsSubscription = null;
                    Debug.WriteLine("NTF subscription CANCELED");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NTF subscription CANCEL FAILED: " + ex.Message);
                }
            }
        }

        async Task StopCommandsSubscription()
        {
            if (commandsSubscription != null)
            {
                try
                {
                    Debug.WriteLine("CMD subscription CANCEL");
                    await ClientService.Current.RemoveSubscriptionAsync(commandsSubscription);
                    commandsSubscription = null;
                    Debug.WriteLine("CMD subscription CANCELED");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("CMD subscription CANCEL FAILED: " + ex.Message);
                }
            }
        }

        async Task LoadDevice()
        {
            LoadingItems++;
            try
            {
                Device = await ClientService.Current.GetDeviceAsync(deviceId);
            }
            catch (Exception ex)
            {
                new MessageDialog(ex.Message, "Error").ShowAsync();
            }
            LoadingItems--;
        }

        async void LoadNotifications()
        {
            await StopNotificationsSubscription();
            bool loaded = false;
            NotificationFilter filter = new NotificationFilter()
            {
                End = filterNotificationsEnd,
                Start = filterNotificationsStart,
                SortOrder = SortOrder.DESC
            };
            var list = new IncrementalLoadingCollection<Notification>(async (take, skip) =>
            {
                filter.Skip = (int)skip;
                filter.Take = (int)take;
                try
                {
                    Debug.WriteLine("NTF LOAD START");
                    var notifications = await ClientService.Current.GetNotificationsAsync(deviceId, filter);
                    Debug.WriteLine("NTF LOAD END");
                    return notifications;
                }
                catch (Exception ex)
                {
                    Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        new MessageDialog(ex.Message, "Error").ShowAsync();
                    });
                    throw ex;
                }
            }, 20);
            list.IsLoadingChanged += (s, isLoading) =>
            {
                LoadingItems += isLoading ? 1 : -1;
                if (!isLoading && !loaded)
                {
                    StartNotificationsSubscription();
                    if (s.Count > 0)
                    {
                        // makes server response faster
                        filter.End = s.First().Timestamp;
                    }
                    loaded = true;
                }
            };
            NotificationsObservable = list;
        }

        async Task StartNotificationsSubscription()
        {
            // TODO check that already started
            if (filterNotificationsEnd != null)
            {
                return;
            }
            await StopNotificationsSubscription();

            Debug.WriteLine("NTF subscription START");
            try
            {
                notificationsSubscription = await ClientService.Current.AddNotificationSubscriptionAsync(new[] { deviceId }, null, async (notificationReceived) =>
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        lock (NotificationsObservable)
                        {
                            if (!NotificationsObservable.Any(c => c.Id == notificationReceived.Notification.Id))
                            {
                                NotificationsObservable.Insert(0, notificationReceived.Notification);
                            }
                        }
                    });
                });
                Debug.WriteLine("NTF subscription END");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("NTF subscription FAILED: " + ex.Message);
                if (!wasSubscriptionError)
                {
                    new MessageDialog(ex.Message, "Can't subscribe to notifications").ShowAsync();
                    wasSubscriptionError = true;
                }
            }
        }

        async void LoadCommands()
        {
            await StopCommandsSubscription();
            bool loaded = false;
            CommandFilter filter = new CommandFilter() 
            {
                End = filterCommandsEnd,
                Start = filterCommandsStart,
                SortOrder = SortOrder.DESC
            };
            var list = new IncrementalLoadingCollection<Command>(async (take, skip) =>
            {
                filter.Skip = (int)skip;
                filter.Take = (int)take;
                try
                {
                    Debug.WriteLine("CMD LOAD START");
                    var commands = await ClientService.Current.GetCommandsAsync(deviceId, filter);
                    Debug.WriteLine("CMD LOAD END");
                    return commands;
                }
                catch (Exception ex)
                {
                    Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        new MessageDialog(ex.Message, "Error").ShowAsync();
                    });
                    throw ex;
                }
            }, 20);
            list.IsLoadingChanged += (s, isLoading) =>
            {
                LoadingItems += isLoading ? 1 : -1;
                if (!isLoading && !loaded)
                {
                    StartCommandsSubscription();
                    if (s.Count > 0)
                    {
                        // makes server response faster
                        filter.End = s.First().Timestamp;
                    }
                    loaded = true;
                }
            };
            CommandsObservable = list;
        }

        async Task StartCommandsSubscription()
        {
            if (CommandsObservable == null || filterCommandsEnd != null)
            {
                return;
            }
            await StopCommandsSubscription();
            
            Debug.WriteLine("CMD subscription START");
            try
            {
                commandsSubscription = await ClientService.Current.AddCommandSubscriptionAsync(new[] { deviceId }, null, async (commandReceived) =>
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        lock (CommandsObservable)
                        {
                            if (!CommandsObservable.Any(c => c.Id == commandReceived.Command.Id))
                            {
                                CommandsObservable.Insert(0, commandReceived.Command);
                            }
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                if (!wasSubscriptionError)
                {
                    new MessageDialog(ex.Message, "Can't subscribe to commands").ShowAsync();
                    wasSubscriptionError = true;
                }
            }
            Debug.WriteLine("CMD subscription END");
        }

        async Task LoadEquipment()
        {
            LoadingItems++;
            try
            {
                GC.Collect();
                Debug.WriteLine("EQP LOAD START");
                var equipment = Device.DeviceClass.Equipment;
                var equipmentState = await ClientService.Current.GetEquipmentStateAsync(deviceId);
                Debug.WriteLine("EQP LOAD END");
                var equipmentInfo = new List<EquipmentStateInfo>();
                foreach (DeviceEquipmentState state in equipmentState)
                {
                    equipmentInfo.Add(new EquipmentStateInfo()
                    {
                        Equipment = equipment.Find(eq => eq.Code == state.Id),
                        State = state
                    });
                }
                DefaultViewModel["Equipment"] = equipmentInfo;
            }
            catch (Exception ex)
            {
                new MessageDialog(ex.Message, "Error").ShowAsync();
            }
            LoadingItems--;
        }

        private void Tabs_Checked(object sender, RoutedEventArgs e)
        {
            // Mirror the change into the CollectionViewSource used by the corresponding ComboBox
            // to ensure that the change is reflected when snapped
            if (tabsViewSource.View != null)
            {
                var filter = (sender as FrameworkElement).DataContext;
                tabsViewSource.View.MoveCurrentTo(filter);
            }
        }

        private sealed class EquipmentStateInfo : DeviceEquipmentState
        {
            public Equipment Equipment { get; set; }
            public DeviceEquipmentState State { get; set; }
        }

        private sealed class Tab : BindableBase
        {
            private String _name;
            private int _count;
            private bool _active;
            private string _visualStateName;

            public Tab(String name, string visualStateName, bool active = false)
            {
                this.VisualStateName = visualStateName;
                this.Name = name;
                this.Active = active;
            }

            public override String ToString()
            {
                return Description;
            }

            public string VisualStateName
            {
                get { return _visualStateName; }
                private set { _visualStateName = value; }
            }

            public String Name
            {
                get { return _name; }
                set { if (this.SetProperty(ref _name, value)) this.OnPropertyChanged("Description"); }
            }

            public int Count
            {
                get { return _count; }
                set { if (this.SetProperty(ref _count, value)) this.OnPropertyChanged("Description"); }
            }

            public bool Active
            {
                get { return _active; }
                set { this.SetProperty(ref _active, value); }
            }

            public Action<object> Filter { get; set; }

            public Action Refresh { get; set; }

            public Action Select { get; set; }

            public Action Deselect { get; set; }

            public String Description
            {
                get
                {
                    if (Count > 0)
                    {
                        return String.Format("{0} ({1})", Name, Count);
                    }
                    else
                    {
                        return Name;
                    }
                }
            }
        }

        private void Tab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var deselectedTab = e.RemovedItems.FirstOrDefault() as Tab;
            if (deselectedTab != null)
            {
                deselectedTab.Active = false;
                if (deselectedTab.Deselect != null)
                {
                    deselectedTab.Deselect();
                }
            }

            var selectedTab = e.AddedItems.FirstOrDefault() as Tab;
            if (selectedTab != null)
            {
                currentTab = selectedTab;
                selectedTab.Active = true;

                filterButton.Visibility = selectedTab.Filter != null ? Visibility.Visible : Visibility.Collapsed;
                refreshButton.Visibility = selectedTab.Refresh != null ? Visibility.Visible : Visibility.Collapsed;
                if (selectedTab.Select != null)
                {
                    selectedTab.Select();
                }

                VisualStateManager.GoToState(this, selectedTab.VisualStateName, true);
            }
        }

        private void Refresh_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (currentTab != null && currentTab.Refresh != null)
            {
                currentTab.Refresh();
            }
        }

        private void Filter_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (currentTab != null && currentTab.Filter != null)
            {
                currentTab.Filter(sender);
            }
        }

        private void SendCommand_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Flyout flyOut = new Flyout();
            flyOut.Placement = FlyoutPlacementMode.Top;

            flyOut.FlyoutPresenterStyle = new Style(typeof(FlyoutPresenter));
            flyOut.FlyoutPresenterStyle.Setters.Add(new Setter(FlyoutPresenter.RequestedThemeProperty, ElementTheme.Dark));
            flyOut.FlyoutPresenterStyle.Setters.Add(new Setter(FlyoutPresenter.PaddingProperty, 10));
            
            StackPanel panel = new StackPanel();
            panel.Width = 300;
            panel.Margin = new Thickness(10);
            panel.Orientation = Orientation.Vertical;
            panel.Children.Add(new TextBlock() { Text = "Command name", FontSize = 14.8 });
            TextBox commandName = new TextBox();
            panel.Children.Add(commandName);
            panel.Children.Add(new TextBlock() { Text = "Params", FontSize = 14.8, Margin = new Thickness(0, 10, 0, 0) });
            TextBox commandParams = new TextBox();
            panel.Children.Add(commandParams);
            panel.Children.Add(new TextBlock() { Text = "JSON or empty string" });
            Button sendButton = new Button() { Content = "Send", Margin = new Thickness(0, 10, 0, 0) };
            panel.Children.Add(sendButton);

            if (CommandSelected != null)
            {
                commandName.Text = CommandSelected.Name;
                commandParams.Text = (string)new ObjectToJsonStringConverter().Convert(CommandSelected.Parameters, null, null, null);
            }

            sendButton.Command = new DelegateCommand(async () =>
            {
                if (commandName.Text.Trim() == "")
                {
                    new MessageDialog("Empty command name", "Send command").ShowAsync();
                    return;
                }
                panel.ControlsEnable(false);
                LoadingItems++;
                try
                {
                    var command = new Command(commandName.Text, commandParams.Text != "" ? JObject.Parse(commandParams.Text) : null);
                    Debug.WriteLine("CMD SEND START");
                    await ClientService.Current.SendCommandAsync(deviceId, command, CommandResultCallback);
                    Debug.WriteLine("CMD SEND END");
                    flyOut.Hide();
                }
                catch (Exception ex)
                {
                    new MessageDialog(ex.Message, "Send command").ShowAsync();
                }
                panel.ControlsEnable(true);
                LoadingItems--;
            });

            flyOut.Content = panel;
            flyOut.ShowAt((FrameworkElement)sender);
        }

        async void CommandResultCallback(Command command)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                lock (commandsObservable)
                {
                    foreach (Command cmd in CommandsObservable)
                    {
                        if (command.Id == command.Id)
                        {
                            // Command class doesn't implement INotifyPropertyChanded to update its result,
                            // so old command is replaced by command with result:
                            var index = commandsObservable.IndexOf(cmd);
                            commandsObservable.RemoveAt(index);
                            commandsObservable.Insert(index, command);
                            break;
                        }
                    }
                }
            });
        }
        
        private void ListView_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            CommandSelected = null;
            if (e.AddedItems.Any())
            {
                var command = e.AddedItems[0] as Command;
                if (command != null)
                {
                    CommandSelected = command;
                }
            }
        }

    }
}
