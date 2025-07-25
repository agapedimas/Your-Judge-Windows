using Your_Judge.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel.Contacts;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static Your_Judge.Pages.Challenges;

namespace Your_Judge.Pages
{
    public sealed partial class Challenges : Page
    {
        public DateTime? LastRefresh;
        public Challenges()
        {
            InitializeComponent();
            Collection_Challenge.Source = Challenge.List;
            Refresh();
            Window.Current.Activated += (s, e) =>
            {
                if (e.WindowActivationState == CoreWindowActivationState.CodeActivated || e.WindowActivationState == CoreWindowActivationState.PointerActivated)
                    Refresh();
            };
        }
        async void Refresh()
        {
            if (LastRefresh != null && DateTime.Now.Subtract((DateTime)LastRefresh) <= TimeSpan.FromSeconds(30))
                return;

            LastRefresh = DateTime.Now;
            await User.Fetch();
            await Challenge.Fetch();
        }
        private void ListView_Challenges_Click(object sender, ItemClickEventArgs e)
        {
            var challenge = (Challenge)e.ClickedItem;
            MainPage.MainNavigation.SelectedItem = MainPage.MainNavigation.MenuItems[0];
            MainPage.MainFrame.Navigate(typeof(Code), challenge, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight});
        }

        internal class Database
        {
            public string Id { get; set; }
            public string FilePath { get; set; }
            public string FileToken{ get; set; }
            public string Results { get; set; }
        }
        private void Image_Failed(object sender, ExceptionRoutedEventArgs e)
        {
            ((Image)sender).Source = new BitmapImage(Variables.DefaultAvatar);
        }
    }
    public class Challenge : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public override string ToString()
        {
            return Name;
        }
        public string AuthorId 
        {
            set => Author = new User { Id = value }; 
        }
        private User _Author { get; set; }
        public User Author
        {
            get => _Author;
            set
            {
                _Author = User.GetById(value.Id);
                NotifyPropertyChanged("Author");
            }
        }
        public string Id { get; set; }
        private string _Name { get; set; }
        public string Name
        {
            get => _Name;
            set
            {
                _Name = value;
                NotifyPropertyChanged("Name");
            }
        }
        private string _Topic { get; set; } = "";
        public string Topic
        {
            get => _Topic;
            set
            {
                _Topic = value;
                NotifyPropertyChanged("Topic");
            }
        }
        private string _Snippet { get; set; } = "";
        public string Snippet
        {
            get => _Snippet;
            set
            {
                _Snippet = value;
                NotifyPropertyChanged("Snippet");
            }
        }
        public string? Problem { get; set; }
        private string _Time { get; set; } = "0";
        private ulong _Timestamp { get; set; }
        public string Time
        {
            get
            {
                if (_Timestamp == 0 && ulong.TryParse(_Time, out _))
                    _Timestamp = ulong.Parse(_Time);

                DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime time = epoch + TimeSpan.FromMilliseconds(_Timestamp);
                return time.ToString("MMMM d, yyyy");
            }
            set { _Time = value; }
        }
        private int _Cases { get; set; } = 0;
        public int Cases
        {
            get => _Cases;
            set
            {
                if (_Cases == value)
                    return;

                _Cases = value;
                NotifyPropertyChanged("Cases");
                NotifyPropertyChanged("Results");
                NotifyPropertyChanged("Score");
            }
        }
        private List<ChallengeResult> _Results { get; set; } = [];
        public List<ChallengeResult> Results
        {
            get
            {
                if (Cases == 0)
                {
                    return [];
                }
                else if (_Results.Count != Cases)
                {
                    _Results.Clear();
                    for (int i = 1; i <= Cases; i++)
                        _Results.Add(new() { Index = i, Parent = this });
                }

                return _Results;
            }
            set
            {
                if (_Results == value)
                    return;

                _Results = value;
                NotifyPropertyChanged("Results");
            }
        }
        private string _Code { get; set; }
        public string Code 
        { 
            get
            {
                if (string.IsNullOrEmpty(_Code))
                {
                    var language = Variables.SelectedProgrammingLanguage;

                    if (language != null)
                        return language.DefaultCode.Replace("<#? defaultclass ?#>", Regex.Replace(Name, @"[^a-zA-Z]", ""));
                    else
                        return "";
                }

                return _Code.Trim();
            } 
            set 
            {
                _Code = value;
                NotifyPropertyChanged("Code");
            } 
        }
        public string? _FilePath { get; set; }
        public string? FilePath 
        { 
            get => _FilePath; 
            set
            {
                _FilePath = value;
                NotifyPropertyChanged("FilePath");
                NotifyPropertyChanged("FileName");
            }
        }
        public string FileName
        {
            get
            {
                if (string.IsNullOrEmpty(FilePath))
                    return "No file selected";


                string[] paths = FilePath.Split("\\");
                return paths[paths.Length - 2] + "/" + paths[paths.Length - 1];
            }
        }
        public string? FileToken { get; set; }
        public int Solved { get { return Results.Where(o => o.Status == ChallengeResult.Type.Correct).Count(); } }
        public int Score 
        { 
            get 
            {
                if (Cases > 0)
                    return (int)(Solved * 1.0 / Cases * 100);
                else
                    return 0;
            } 
        }
        public static ObservableCollection<Challenge> List { get; set; } = [];
        public static async Task Fetch(bool fromDatabase = false)
        {
            var pendingChallengeList = List.ToDictionary(o => o.Id);
            var challengeList = Data.DatabaseGet<Challenge>("Challenges", ["Id", "AuthorId", "Name", "Topic", "Snippet", "Time", "Cases"]);
            var progressList = Data.DatabaseGet<Database>("Progress", ["Id", "FilePath", "FileToken", "Results"]);

            if (fromDatabase == false)
            {
                var result = await Data.HttpPost("/challenges/get");
                if (result.Status == Data.HttpResult.HttpStatus.Success)
                {
                    await Data.DatabaseRemove("Challenges");
                    if (result.Data == null)
                    {
                        challengeList = new();
                    }
                    else
                    {
                        challengeList = Data.ConvertToList<Challenge>(result.Data);

                        var list = new List<string[]>();

                        foreach (var challenge in challengeList)
                        {
                            list.Add([challenge.Id, challenge.Author.Id, challenge.Name, challenge.Topic, challenge.Snippet, challenge._Time, challenge.Cases.ToString()]);
                        }

                        Data.DatabaseSet("Challenges", "Id", ["Id", "AuthorId", "Name", "Topic", "Snippet", "Time", "Cases"], list);
                    }
                }
            }

            foreach (Challenge newItem in challengeList)
            {
                if (pendingChallengeList.TryGetValue(newItem.Id, out var oldItem))
                {
                    if (newItem.Author != oldItem.Author || newItem.Name != oldItem.Name || newItem.Topic != oldItem.Topic || newItem.Snippet != oldItem.Snippet || newItem.Cases != oldItem.Cases)
                    {
                        var index = List.IndexOf(oldItem);
                        var selectedItem = List[index];

                        selectedItem.Author = newItem.Author;
                        selectedItem.Name = newItem.Name;
                        selectedItem.Topic = newItem.Topic;
                        selectedItem.Snippet = newItem.Snippet;
                        selectedItem.Cases = newItem.Cases;
                    }

                    pendingChallengeList.Remove(newItem.Id);
                }
                else
                {
                    var progress = progressList.SingleOrDefault(o => o.Id == newItem.Id);
                    if (progress != null)
                    {
                        newItem.FilePath = progress.FilePath;
                        newItem.FileToken = progress.FileToken;
                        newItem.Results = Data.ConvertToList<ChallengeResult>(progress.Results);

                        foreach (var challengeResult in newItem.Results)
                            challengeResult.Parent = newItem;
                    }

                    List.Add(newItem);
                }
            }

            foreach (var deletedItem in pendingChallengeList.Values)
                List.Remove(deletedItem);

            for (int i = 0; i < challengeList.Count; i++)
            {
                var newItem = challengeList[i];
                var existingIndex = List.ToList().FindIndex(o => o.Id == newItem.Id);

                if (existingIndex != -1 && existingIndex != i)
                {
                    var movedItem = List[existingIndex];
                    List.RemoveAt(existingIndex);
                    List.Insert(i, movedItem);
                }
            }
        }
    }
    public class ChallengeResult : INotifyPropertyChanged
    {
        public Challenge? Parent { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public void NotifyPropertyChanged_Parent(string propertyName)
        {
            Parent?.NotifyPropertyChanged(propertyName);
        }
        public int Index { get; set; }
        public enum Type { Correct, Wrong, Error, Compiling, None };
        private Type _Status { get; set; } = Type.None;
        public Type Status 
        { 
            get => _Status; 
            set 
            {
                _Status = value;
                NotifyPropertyChanged("Status");
                NotifyPropertyChanged("Icon");
                NotifyPropertyChanged("Title");
                NotifyPropertyChanged("Background");
                NotifyPropertyChanged("Foreground");
                NotifyPropertyChanged("IsLoading");
                NotifyPropertyChanged("ErrorButtonVisibility");
                NotifyPropertyChanged_Parent("Solved");
                NotifyPropertyChanged_Parent("Score");
            } 
        }
        public bool IsLoading
        {
            get => Status == Type.Compiling;
        }
        public string Icon
        {
            get 
            {
                if (Status == Type.Wrong)
                    return "\uf13d";
                else if (Status == Type.Correct)
                    return "\uf13e";
                else if (Status == Type.Error)
                    return "\uf13c";
                else
                    return "";
            }
        }
        internal class BackgroundColor
        {
            internal static readonly byte alpha = 255;
            public static readonly SolidColorBrush Correct_Light = new SolidColorBrush(Color.FromArgb(alpha, 223, 246, 221));
            public static readonly SolidColorBrush Correct_Dark = new SolidColorBrush(Color.FromArgb(alpha, 57, 61, 27));
            public static readonly SolidColorBrush Wrong_Light = new SolidColorBrush(Color.FromArgb(alpha, 253, 231, 233));
            public static readonly SolidColorBrush Wrong_Dark = new SolidColorBrush(Color.FromArgb(alpha, 68, 39, 38));
            public static readonly SolidColorBrush Error_Light = new SolidColorBrush(Color.FromArgb(alpha, 255, 244, 206));
            public static readonly SolidColorBrush Error_Dark = new SolidColorBrush(Color.FromArgb(alpha, 67, 53, 25));
            public static readonly SolidColorBrush None = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }
        public Brush Background
        {
            get
            {
                bool dark = Application.Current.RequestedTheme == ApplicationTheme.Dark;

                if (Status == Type.Error)
                    return dark ? BackgroundColor.Error_Dark : BackgroundColor.Error_Light;
                else if (Status == Type.Correct)
                    return dark ? BackgroundColor.Correct_Dark : BackgroundColor.Correct_Light;
                else if (Status == Type.Wrong)
                    return dark ? BackgroundColor.Wrong_Dark : BackgroundColor.Wrong_Light;
                else
                    return BackgroundColor.None;
            }
        }
        internal class ForegroundColor
        {
            internal static readonly byte alpha = 255;
            public static readonly SolidColorBrush Correct_Light = new SolidColorBrush(Color.FromArgb(alpha, 15, 123, 15));
            public static readonly SolidColorBrush Correct_Dark = new SolidColorBrush(Color.FromArgb(alpha, 108, 203, 95));
            public static readonly SolidColorBrush Wrong_Light = new SolidColorBrush(Color.FromArgb(alpha, 196, 43, 28));
            public static readonly SolidColorBrush Wrong_Dark = new SolidColorBrush(Color.FromArgb(alpha, 255, 153, 164));
            public static readonly SolidColorBrush Error_Light = new SolidColorBrush(Color.FromArgb(alpha, 157, 93, 0));
            public static readonly SolidColorBrush Error_Dark = new SolidColorBrush(Color.FromArgb(alpha, 252, 225, 0));
            public static readonly SolidColorBrush None = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }
        public Brush Foreground
        {
            get
            {
                bool dark = Application.Current.RequestedTheme == ApplicationTheme.Dark;

                if (Status == Type.Error)
                    return dark ? ForegroundColor.Error_Dark : ForegroundColor.Error_Light;
                else if (Status == Type.Correct)
                    return dark ? ForegroundColor.Correct_Dark : ForegroundColor.Correct_Light;
                else if (Status == Type.Wrong)
                    return dark ? ForegroundColor.Wrong_Dark : ForegroundColor.Wrong_Light;
                else
                    return ForegroundColor.None;
            }
        }
        public string Title
        {
            get
            {
                if (Status == Type.Error)
                    return ErrorType.ToUpper();
                else if (Status == Type.Wrong)
                    return "WRONG";
                else if (Status == Type.Correct)
                    return "OK";
                else if (Status == Type.Compiling)
                    return "RUNNING";
                else
                    return "NOT TESTED";
            }
        }
        private string _ErrorType { get; set; } = "";
        public string ErrorType
        {
            get => _ErrorType;
            set
            {
                _ErrorType = value; 
                NotifyPropertyChanged("ErrorType");
                NotifyPropertyChanged("Title");
            }
        }
        private string _ErrorMessage { get; set; } = "";
        public string ErrorMessage
        {
            get => _ErrorMessage;
            set
            {
                _ErrorMessage = value;
                NotifyPropertyChanged("ErrorMessage");
            }
        }
        private string _ErrorDetails { get; set; } = "";
        public string ErrorDetails
        {
            get => _ErrorDetails;
            set
            {
                _ErrorDetails = value;
                NotifyPropertyChanged("ErrorDetails");
            }
        }
        public Visibility ErrorButtonVisibility { get => Status == Type.Error ? Visibility.Visible : Visibility.Collapsed; }
    }
    public class User : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private string _Id { get; set; }
        public string Id
        {
            get => _Id;
            set
            {
                _Id = value;
                NotifyPropertyChanged("Id");
            }
        }
        private string _Username { get; set; } = "unknown";
        public string Username
        {
            get => _Username;
            set 
            {
                _Username = value;
                NotifyPropertyChanged("Username");
            }
        }
        private string _Nickname { get; set; } = "Unknown";
        public string Nickname
        {
            get => _Nickname;
            set
            {
                _Nickname = value;
                NotifyPropertyChanged("Nickname");
            }
        }
        public string URL { get; set; } = Variables.Host;
        public string Avatar
        { 
            get
            {
                if (Role == UserRole.Administrator)
                    return "https://" + Variables.Host + "/avatar/" + Id;
                else
                    return "https://" + Variables.Host + "/avatar/default.webp";
            }
        }
        public enum UserRole { User, Administrator }
        public UserRole Role { get; set; }
        public static ObservableCollection<User> List { get; set; } = [];
        public static User GetById(string id)
        {
            var result = List.SingleOrDefault(o => o.Id == id);

            if (result == null)
                return new User();
            else
                return result;
        }
        public static async Task Fetch(bool fromDatabase = false)
        {
            List<User> authorList = Data.DatabaseGet<User>("Authors", ["Id", "Username", "Nickname", "URL"]); ;

            if (fromDatabase == false)
            {
                var result = await Data.HttpPost("/authors/get");
                if (result.Status == Data.HttpResult.HttpStatus.Success)
                {
                    await Data.DatabaseRemove("Authors");
                    if (result.Data == null)
                    {
                        authorList = new();
                    }
                    else
                    {
                        authorList = Data.ConvertToList<User>(result.Data);

                        var list = new List<string[]>();

                        foreach (var author in authorList)
                            list.Add([author.Id, author.Username, author.Nickname, author.URL]);

                        Data.DatabaseSet("Authors", "Username", ["Id", "Username", "Nickname", "URL"], list);
                    }
                }
            }

            List.Clear();

            foreach (User author in authorList)
            {
                author.Role = UserRole.Administrator;
                List.Add(author);
            }
        }
    }
}
