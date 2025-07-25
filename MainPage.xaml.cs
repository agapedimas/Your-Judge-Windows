using Your_Judge.Classes;
using Your_Judge.Pages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using MUXC = Microsoft.UI.Xaml.Controls;

namespace Your_Judge
{
    public sealed partial class MainPage : Page
    {
        public static Frame MainFrame;
        public static MUXC.NavigationView MainNavigation; 
        private CancellationTokenSource? CancelSearchToken;
        public MainPage()
        {
            InitializeComponent();
            MainNavigation = Navigation;
            MainFrame = Frame;

            Frame.Navigated += (s, e) =>
            {
                TitleBarAnimationKey.From = TitleBarTransform.X;
                if (e.SourcePageType == typeof(Code))
                {
                    Button_Back.Visibility = Visibility.Visible;
                    TitleBarAnimationKey.To = 32;
                }
                else
                {
                    Button_Back.Visibility = Visibility.Collapsed;
                    TitleBarAnimationKey.To = 0;
                }
                TitleBarAnimation.Begin();
            };

            Button_Back.Click += (s, e) =>
            {
                if (Frame.CanGoBack)
                    Frame.GoBack();
            };

            {
                // Set title bar
                var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
                coreTitleBar.ExtendViewIntoTitleBar = true;

                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                titleBar.ButtonBackgroundColor = Colors.Transparent;

                Window.Current.SetTitleBar(TitleBar);
            }

            foreach (PageInfo page in Classes.Navigation.Pages)
            {
                var menu = new MUXC.NavigationViewItem()
                {
                    Content = page.Title,
                    Icon = new FontIcon() { Glyph = page.Icon },
                    Tag = page.Type
                };

                if (page.Placement == PageInfo.Location.Footer)
                    Navigation.FooterMenuItems.Add(menu);
                else
                    Navigation.MenuItems.Add(menu);
            }

            Navigation.SelectedItem = Navigation.MenuItems[0];

            string lastQuery = "";
            Search.TextChanged += async (sender, args) =>
            {
                if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
                    return;

                CancelSearchToken?.Cancel();
                CancelSearchToken = new();
                var token = CancelSearchToken.Token;

                if (sender.Text.Trim().Equals(lastQuery))
                    return;

                if (string.IsNullOrEmpty(sender.Text))
                {
                    sender.ItemsSource = new List<Challenge>();
                    return;
                }

                try
                {
                    await Task.Delay(400, token);

                    object query = new { query = sender.Text.Trim() };
                    lastQuery = sender.Text.Trim();
                    string json = JsonConvert.SerializeObject(query);
                    var result = await Data.HttpPost("/challenges/get", json);

                    if (result.Status == Data.HttpResult.HttpStatus.Error || result.Data == null)
                    {
                        sender.ItemsSource = null;
                        return;
                    }

                    List<Challenge> challenges = Data.ConvertToList<Challenge>(result.Data);

                    if (token.IsCancellationRequested) 
                        return;

                    if (challenges.Count == 0)
                        challenges.Add(new() { Topic = "No results found" });

                    sender.ItemsSource = challenges;
                }
                catch {}
            };

            Search.QuerySubmitted += (sender, args) =>
            {
                var challenge = (Challenge)args.ChosenSuggestion;

                if (challenge == null || challenge.Id == null)
                    return;

                challenge = Challenge.List.SingleOrDefault(o => o.Id.Equals(challenge.Id));

                if (challenge == null)
                    return;

                Navigation.SelectedItem = Navigation.MenuItems[0];
                Frame.Navigate(typeof(Code), challenge, new DrillInNavigationTransitionInfo());
            };
        }
        private void Navigation_SelectionChanged(MUXC.NavigationView sender, MUXC.NavigationViewSelectionChangedEventArgs args)
        {
            var item = (MUXC.NavigationViewItem)args.SelectedItem;

            if (item != null)
            {
                if (item.Tag is string && (string)item.Tag == "Settings")
                    Frame.Navigate(typeof(Settings));
                else if (Frame.CurrentSourcePageType == typeof(Code) && (Type)item.Tag == typeof(Challenges))
                    Frame.Navigate((Type)item.Tag, null, new DrillInNavigationTransitionInfo());
                else
                    Frame.Navigate((Type)item.Tag);
            }
        }
        private void Navigation_ItemInvoked(MUXC.NavigationView sender, MUXC.NavigationViewItemInvokedEventArgs args)
        {
            var item = (string)args.InvokedItem;

            if (item != null)
            {
                if (Frame.CurrentSourcePageType == typeof(Code) && item == "Challenges")
                    Frame.Navigate(typeof(Challenges), null, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft });
            }
        }
    }
}
