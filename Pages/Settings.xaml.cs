using Your_Judge.Classes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Management.Deployment;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Your_Judge.Pages
{
    public sealed partial class Settings : Page
    {
        public List<ProgrammingLanguage> programmingLanguages;
        public Settings()
        {
            this.InitializeComponent();
            programmingLanguages = ProgrammingLanguage.Available;

            for (int i = 0; i < programmingLanguages.Count; i++)
            {
                if (programmingLanguages[i].Alias == Variables.SelectedProgrammingLanguage.Alias)
                {
                    Select_ProgrammingLanguage.SelectedIndex = i;
                    break;
                }
            }

            Select_ProgrammingLanguage.SelectionChanged += (s, e) =>
            {
                var selected = (ProgrammingLanguage)e.AddedItems[0];
                Variables.SelectedProgrammingLanguage = selected;
            };
        }
        private async void Click_DefaultEditor(object sender, RoutedEventArgs e)
        {
            _ = await Launcher.LaunchUriAsync(new("ms-settings:defaultapps"));
        }
        private async void Click_ResetProgress(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog()
            {
                Title = "Reset all progress?",
                Content = "This will clear all your unsaved code.",
                PrimaryButtonText = "Reset",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Secondary,
            };
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await Data.DatabaseRemove("Progress");
                await Challenge.Fetch(fromDatabase: true);
            }
        }
        private async void Click_CompilerRepository(object sender, RoutedEventArgs e)
        {
            _ = await Launcher.LaunchUriAsync(new("https://github.com/engineer-man/piston"));
        }
        private async void Click_AppRepository(object sender, RoutedEventArgs e)
        {
            _ = await Launcher.LaunchUriAsync(new("https://github.com/agapedimas/Judge-Windows"));
        }
    }
}
