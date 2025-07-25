using CommunityToolkit.WinUI.Controls;
using Your_Judge.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Your_Judge.Pages
{
    public sealed partial class Help : Page
    {
        public Help()
        {
            this.InitializeComponent();

            var references = new List<Reference>();
            references.Add(new Reference
            {
                Name = "W3Schools",
                Icon = Variables.DefaultAssetsPath + "/W3Schools.png",
                URL = "https://w3schools.com"
            });
            references.Add(new Reference
            {
                Name = "Competitive Programming Book",
                Icon = Variables.DefaultAssetsPath + "/Omega.png",
                URL = "https://cpbook.net/"
            });
            references.Add(new Reference
            {
                Name = "ChatGPT",
                Icon = Variables.DefaultAssetsPath + "/ChatGPT.png",
                URL = "https://chatgpt.com"
            });

            Collection_Administrator.Source = User.List;
            Collection_Reference.Source = references;
        }
        private async void Click_Administrator(object sender, RoutedEventArgs e)
        {
            var card = (SettingsCard)sender;
            var user = (User)card.Tag;

            _ = await Launcher.LaunchUriAsync(new(user.URL));
        }
        private async void Click_Reference(object sender, RoutedEventArgs e)
        {
            var card = (SettingsCard)sender;
            var reference = (Reference)card.Tag;

            _ = await Launcher.LaunchUriAsync(new(reference.URL));
        }
        private void Image_Failed(object sender, ExceptionRoutedEventArgs e)
        {
            ((Image)sender).Source = new BitmapImage(Variables.DefaultAvatar);
        }
    }
    public class Reference
    {
        public string? Name { get; set; }
        public string? Icon { get; set; }
        public string? URL { get; set; }
    }
}
