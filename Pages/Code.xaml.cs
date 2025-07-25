using Your_Judge.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using muxc = Microsoft.UI.Xaml.Controls;

namespace Your_Judge.Pages
{
    public sealed partial class Code : Page
    {
        public Challenge SelectedChallenge = new();
        public CodeVisual Visual;
        public DateTimeOffset? LastModified;
        public AppWindow? PIPWindow;
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            SelectedChallenge = (Challenge)e.Parameter;
            Visual = new CodeVisual(SelectedChallenge);
            Visual.IsFetching = true;

            var file = await Editor_GetFile();

            if (file != null)
            {
                var props = await file.GetBasicPropertiesAsync();
                LastModified = props.DateModified;

                await Editor_ReadFile(file);
            }
        }
        public Code()
        {
            this.InitializeComponent();
        }
        async void Editor_Submit()
        {
            foreach (var Result in SelectedChallenge.Results)
            {
                Result.ErrorType = "";
                Result.ErrorMessage = "";
                Result.ErrorDetails = "";
                Result.Status = ChallengeResult.Type.Compiling;
            }

            var code= HttpUtility.UrlEncode(SelectedChallenge.Code);
            await Data.HttpGetStream("/challenges/test/" + SelectedChallenge.Id + "?language=" + Variables.SelectedProgrammingLanguage + "&code=" + code, async (data) =>
            {
                if (data == "DONE")
                {
                    await Editor_Save(SaveType.Results);
                }
                else
                {
                    var result = Data.ConvertToType<Result>(data);
                    var item = SelectedChallenge.Results[result.Number - 1];

                    if (result.Status == "OK")
                        item.Status = ChallengeResult.Type.Correct;
                    else if (result.Status == "WRONG")
                        item.Status = ChallengeResult.Type.Wrong;
                    else
                        item.Status = ChallengeResult.Type.Error;
                    
                    if (item.Status == ChallengeResult.Type.Error)
                    {
                        if (result.Error == "COMPILER UNAVAILABLE")
                        {
                            item.ErrorType = "Server Error";
                            item.ErrorMessage = "We've encountered an issue with the compiler's server. Please try again later.";
                        }
                        else if (string.IsNullOrEmpty(result.Error) == false)
                        {
                            item.ErrorType = "Code Error";
                            item.ErrorMessage = "An error occurred with your code. Please check before resubmit to judge.";
                            item.ErrorDetails = result.Error;
                        }
                        else
                        {
                            item.ErrorType = "Unknown Error";
                            item.ErrorMessage = "An unknown error occurred. Please try again later.";
                        }
                    }
                }
            });
        }
        internal enum SaveType { Results, FilePath, FileCode };
        async Task Editor_Save(SaveType type)
        {
            if (type == SaveType.Results)
            {
                var array = new List<object>();

                for (int i = 0; i < SelectedChallenge.Results.Count; i++)
                {
                    var result = SelectedChallenge.Results[i];
                    string status = "NONE";

                    if (result.Status == ChallengeResult.Type.Correct)
                        status = "CORRECT";
                    else if (result.Status == ChallengeResult.Type.Wrong)
                        status = "WRONG";
                    else if (result.Status == ChallengeResult.Type.Error)
                        status = "ERROR";

                    array.Add(new
                    {
                        ErrorType = result.ErrorType,
                        ErrorMessage = result.ErrorMessage,
                        ErrorDetails = result.ErrorDetails,
                        Status = status,
                        Index = i + 1
                    });
                }
                string results = JsonConvert.SerializeObject(array);

                Data.DatabaseSet(
                    table: "Progress",
                    keyColumn: "Id",
                    columns: ["Id", "Results"],
                    values: [SelectedChallenge.Id, results]
                );
            }
            else if (type == SaveType.FilePath)
            {
                Data.DatabaseSet(
                    table: "Progress",
                    keyColumn: "Id",
                    columns: ["Id", "FilePath", "FileToken"],
                    values: [SelectedChallenge.Id, SelectedChallenge.FilePath, SelectedChallenge.FileToken]
                );
            }
            else if (type == SaveType.FileCode)
            {
                try
                {
                    StorageFile? file = await Editor_GetFile();

                    if (file == null)
                    {
                        var picker = new FileSavePicker();
                        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                        picker.FileTypeChoices.Add(Variables.SelectedProgrammingLanguage.Name + " Source Code", new List<string>() { Variables.SelectedProgrammingLanguage.FileExtension });
                        picker.SuggestedFileName = Regex.Replace(SelectedChallenge.Name, @"[^a-zA-Z]", "");
                        picker.CommitButtonText = "Save";
                        file = await picker.PickSaveFileAsync();

                        if (file == null)
                            return;
                    }

                    string token = StorageApplicationPermissions.FutureAccessList.Add(file);
                    SelectedChallenge.FileToken = token;
                    SelectedChallenge.FilePath = file.Path;

                    await Editor_Save(SaveType.FilePath);
                    await FileIO.WriteTextAsync(file, SelectedChallenge.Code);

                    var props = await file.GetBasicPropertiesAsync();
                    LastModified = props.DateModified;

                    NotificationQueue.Clear();
                    InfoBar.Title = "Saved";
                    InfoBar.Severity = muxc.InfoBarSeverity.Success;
                    NotificationQueue.Show("Your code for this challenge has been saved", 3000);
                }
                catch
                {
                    NotificationQueue.Clear();
                    InfoBar.Title = "Save Failed";
                    InfoBar.Severity = muxc.InfoBarSeverity.Error;
                    NotificationQueue.Show("Something when wrong while saving your file", 3000);
                }
            }
        }
        async Task<StorageFile?> Editor_GetFile()
        {
            StorageFile? file = null;
            if (SelectedChallenge.FileToken != null && StorageApplicationPermissions.FutureAccessList.ContainsItem(SelectedChallenge.FileToken))
            {
                try
                {
                    file = await StorageApplicationPermissions.FutureAccessList.GetFileAsync(SelectedChallenge.FileToken);
                }
                catch
                {
                    SelectedChallenge.FilePath = null;
                    SelectedChallenge.FileToken = null;
                    await Editor_Save(SaveType.FilePath);
                }
            }

            return file;
        }
        async Task Editor_ReadFile(StorageFile file)
        {
            string content = await FileIO.ReadTextAsync(file);

            if (string.IsNullOrEmpty(content) == false)
                SelectedChallenge.Code = content;
        }
        internal class Result
        {
            public int Number { get; set; }
            public string Status { get; set; }
            public string Error { get; set; }
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.Activated += Page_Focus;
        }
        private void Page_Unloaded( object sender, RoutedEventArgs e)
        {
            Window.Current.Activated -= Page_Focus;
        }
        private async void Page_Focus(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == CoreWindowActivationState.CodeActivated || e.WindowActivationState == CoreWindowActivationState.PointerActivated)
            {
                var file = await Editor_GetFile();

                if (file == null)
                    return;

                var props = await file.GetBasicPropertiesAsync();

                if (props.DateModified.Equals(LastModified) == false)
                {
                    LastModified = props.DateModified;
                    await Editor_ReadFile(file);
                }
            }
        }
        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width > 1000)
                Visual.ButtonLabelVisibility = Visibility.Visible;
            else 
                Visual.ButtonLabelVisibility = Visibility.Collapsed;

            if (e.NewSize.Width < 500)
                Visual.IsCompactButtons = true;
            else
                Visual.IsCompactButtons = false;

            if (e.NewSize.Width > 1100)
                Visual.IsCompactLayout = false;
            else
                Visual.IsCompactLayout = true;
        }
        private async void WebView_Loaded(object sender, RoutedEventArgs e)
        {
            WebView problem = (WebView)sender;
            problem.ScriptNotify += (s, e) =>
            {
                string scroll = e.Value;
                bool parsed = int.TryParse(scroll, out int height);

                if (parsed)
                    problem.Height = height;
            };
            problem.NavigationCompleted += (s, e) =>
            {
                Visual.IsFetching = false;
            };

            if (SelectedChallenge.Problem == null)
            {
                var result = await Data.HttpPost("/challenges/get/" + SelectedChallenge.Id);

                if (result.Status == Data.HttpResult.HttpStatus.Error)
                {
                    // error handler
                }

                if (result.Data == null)
                {
                    Visual.IsFetching = false;
                    return;
                }

                Challenge? challenge = Data.ConvertToType<Challenge>(result.Data);

                if (challenge == null)
                {
                    Visual.IsFetching = false;
                    return;
                }

                string? text = challenge.Problem;
                SelectedChallenge.Problem = text;
            }

            string html = Variables.HTMLTemplate(SelectedChallenge.Problem, new Thickness(0, 0, 15, 15));
            problem.NavigateToString(html);
        }
        private async void Click_NewWindow(object sender, RoutedEventArgs e)
        {
            if (PIPWindow == null)
            {
                PIPWindow = await AppWindow.TryCreateAsync();
                PIPWindow.CloseRequested += (s, e) => PIPWindow = null;
            }
            var frame = new Frame();

            if (PIPWindow.IsVisible == false)
            {
                PIPWindow.RequestSize(new Size(425, 900));
                PIPWindow.Title = SelectedChallenge.Name;
                frame.Navigate(typeof(Code_PIP), SelectedChallenge);

                ElementCompositionPreview.SetAppWindowContent(PIPWindow, frame);
            }

            await PIPWindow.TryShowAsync();
        }
        private async void Click_DefaultEditor(object sender, RoutedEventArgs e)
        {
            var file = await Editor_GetFile();

            if (file == null)
                return;

            if (await Launcher.LaunchFileAsync(file))
                return;
        }
        private void Click_CopyLink(object sender, RoutedEventArgs e)
        {
            var dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Link;
            dataPackage.SetText("https://agapedimas.com/j/" + SelectedChallenge.Id);
            Clipboard.SetContent(dataPackage);

            NotificationQueue.Clear();
            InfoBar.Title = "Copied";
            InfoBar.Severity = muxc.InfoBarSeverity.Success;
            NotificationQueue.Show("URL for this challenge has been copied to your clipboard.", 3000);
        }
        private async void Click_SelectFile(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(Variables.SelectedProgrammingLanguage.FileExtension);
            picker.CommitButtonText = "Insert";

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var props = await file.GetBasicPropertiesAsync();
                LastModified = props.DateModified;

                string token = StorageApplicationPermissions.FutureAccessList.Add(file);
                SelectedChallenge.FileToken = token;
                SelectedChallenge.FilePath = file.Path;

                await Editor_ReadFile(file);
                await Editor_Save(SaveType.FilePath);
            }
        }
        private void Click_Submit(object sender, RoutedEventArgs e)
        {
            Editor_Submit();
        }
        private async void Click_Save(object sender, RoutedEventArgs e)
        {
            await Editor_Save(SaveType.FileCode);
        }
        private async void Click_ClearResult(object sender, RoutedEventArgs e)
        {
            foreach (var result in SelectedChallenge.Results)
            {
                result.Status = ChallengeResult.Type.None;
                result.ErrorMessage = "";
                result.ErrorType = "";
            }
    ;
            await Editor_Save(SaveType.Results);
        }
        private async void Click_ShowError(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var result = (ChallengeResult)button.Tag;

            var stackpanel = new StackPanel { Spacing = 20 };
            stackpanel.Children.Add(new TextBlock { Text = result.ErrorMessage, FontSize = 17 });
            stackpanel.Children.Add(new TextBox { AcceptsReturn = true, Text = result.ErrorDetails, FontSize = 15, FontFamily = new FontFamily("Cascadia Mono"), Height = 200, Padding = new Thickness(25, 25, 25, 30), Margin = new Thickness(-25, 0, -25, -30), CornerRadius = new CornerRadius(0) });

            var dialog = new ContentDialog()
            {
                Title = "Error Details",
                Content = stackpanel,
                PrimaryButtonText = "Done"
            };
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;

            await dialog.ShowAsync();
        }
        private void Image_Failed(object sender, ExceptionRoutedEventArgs e)
        {
            ((Image)sender).Source = new BitmapImage(Variables.DefaultAvatar);
        }
    }
    public class CodeVisual : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public Challenge Challenge { get; set; }
        public CodeVisual(Challenge challenge)
        {
            Challenge = challenge;
            Challenge.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName.Equals("FilePath"))
                {
                    NotifyPropertyChanged("SelectFileButtonText");
                    NotifyPropertyChanged("EditButtonVisibility");
                }
                else if (e.PropertyName.Equals("Score"))
                {
                    NotifyPropertyChanged("IsEditorEnabled");
                }
            };
        }
        public string SelectFileButtonText
        {
            get => string.IsNullOrEmpty(Challenge.FilePath) ? "Select File" : "Change";
        }
        public Visibility EditButtonVisibility
        {
            get => string.IsNullOrEmpty(Challenge.FilePath) ? Visibility.Collapsed : Visibility.Visible;
        }
        private Visibility _ButtonLabelVisibility { get; set; }
        public Visibility ButtonLabelVisibility
        {
            get => _ButtonLabelVisibility;
            set
            {
                _ButtonLabelVisibility = value;
                NotifyPropertyChanged("ButtonLabelVisibility");
            }
        }
        private bool _IsCompactButtons { get; set; }
        public bool IsCompactButtons
        {
            get => _IsCompactButtons;
            set
            {
                _IsCompactButtons = value;
                NotifyPropertyChanged("LongButtonsVisibility");
                NotifyPropertyChanged("CompactButtonsVisibility");
            }
        }
        public Visibility LongButtonsVisibility { get => IsCompactButtons ? Visibility.Collapsed : Visibility.Visible; }
        public Visibility CompactButtonsVisibility { get => IsCompactButtons ? Visibility.Visible : Visibility.Collapsed; }
        private bool _IsCompactLayout { get; set; }
        public bool IsCompactLayout
        {
            get => _IsCompactLayout;
            set
            {
                _IsCompactLayout = value;
                NotifyPropertyChanged("ComfortLayoutVisibility");
                NotifyPropertyChanged("CompactLayoutVisibility");
            }
        }
        public Visibility ComfortLayoutVisibility { get => IsCompactLayout ? Visibility.Collapsed : Visibility.Visible; }
        public Visibility CompactLayoutVisibility { get => IsCompactLayout ? Visibility.Visible : Visibility.Collapsed; }
        private bool _IsFetching { get; set; }
        public bool IsFetching
        {
            get => _IsFetching;
            set
            {
                _IsFetching = value;
                NotifyPropertyChanged("IsFetching");
            }
        }
        public bool IsEditorEnabled
        {
            get => Challenge.Results.Exists(o => o.IsLoading) == false;
        }
    }
}
