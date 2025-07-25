using Your_Judge.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class Code_PIP : Page
    {
        public Challenge SelectedChallenge = new();
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            SelectedChallenge = (Challenge)e.Parameter;
        }
        public Code_PIP()
        {
            this.InitializeComponent();
        }
        private void Image_Failed(object sender, ExceptionRoutedEventArgs e)
        {
            ((Image)sender).Source = new BitmapImage(Variables.DefaultAvatar);
        }
        private void WebView_Loaded(object sender, RoutedEventArgs e)
        {
            var problem = (WebView)sender;

            problem.ScriptNotify += (s, e) =>
            {
                string scroll = e.Value;
                bool parsed = int.TryParse(scroll, out int height);

                if (parsed)
                    problem.Height = height;
            };

            string html = Variables.HTMLTemplate(SelectedChallenge.Problem, new Thickness(15, 5, 15, 15));
            problem.NavigateToString(html);
        }
    }
}
