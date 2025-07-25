using Your_Judge.Classes;
using Your_Judge.Pages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using static Your_Judge.Pages.Challenges;

namespace Your_Judge
{
    public sealed partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Data.Initialize();

            Suspending += OnSuspending;
            UnhandledException += (sender, e) =>
            {
                e.Handled = true;
                Debug.WriteLine($"Exception on {e.Exception.Source}: {e.Exception.Message}");
            };
        }
        protected override async void OnActivated(IActivatedEventArgs args)
        {
            await User.Fetch();
            await Challenge.Fetch();
            await Launch();
            if (args.Kind == ActivationKind.Protocol)
            {
                var e = args as ProtocolActivatedEventArgs;
                string uri = e.Uri.AbsoluteUri.Replace("your-judge://", "").ToLower();
                string[] path = uri.Split("/");

                if (path[0] == "challenges")
                {
                    var challenge = Challenge.List.FirstOrDefault(o => o.Id.Equals(path[1]));
                    if (challenge == null)
                        return;

                    MainPage.MainNavigation.SelectedItem = MainPage.MainNavigation.MenuItems[0];
                    MainPage.MainFrame.Navigate(typeof(Code), challenge, new DrillInNavigationTransitionInfo());
                }
            }
        }
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            await User.Fetch(fromDatabase: true);
            await Challenge.Fetch(fromDatabase: true);
            await Launch();
        }
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception($"Failed to load page '{e.SourcePageType.FullName}'.");
        }
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();

            // TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        private bool IsInitialized = false;
        private async Task Launch()
        {
            if (IsInitialized)
                return;

            IsInitialized = true;

            if (Window.Current.Content is not Frame rootFrame)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
                rootFrame.Navigate(typeof(MainPage));

            Window.Current.Activate();
        }
    }
}
