using Your_Judge.Classes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using muxc = Microsoft.UI.Xaml.Controls;

namespace Your_Judge.Pages
{
    public sealed partial class Compiler : Page
    {
        public Editor CodeEditor = new Editor();
        public Compiler()
        {
            this.InitializeComponent();
            Button_Save.Click += (s, e) => Editor_Save();
            Button_Run.Click += (s, e) => Editor_Submit();
            CodeEditor.Code = (string)ApplicationData.Current.LocalSettings.Values["Compiler_Code"];
            CodeEditor.Input = (string)ApplicationData.Current.LocalSettings.Values["Compiler_Input"];
        }
        async void Editor_Submit()
        {
            CodeEditor.IsLoading = true;
            CodeEditor.Output = "Compiling...";

            await Editor_Save(auto: true);

            object code = new { code = CodeEditor.Code, input = CodeEditor.Input };
            string json = JsonConvert.SerializeObject(code);

            var result = await Data.HttpPost("/compile/" + Variables.SelectedProgrammingLanguage + "/", json);

            if (result.Status == Data.HttpResult.HttpStatus.Error)
            {
                CodeEditor.IsLoading = false;
                CodeEditor.Output = "Failed to reach server";
                return;
            }

            if (result.Data == null)
            {
                CodeEditor.Output = "Something went wrong";
            }
            else
            {
                Result? data = Data.ConvertToType<Result>(result.Data);

                if (data == null)
                    CodeEditor.Output = "Something went wrong";
                else if (data.Error != null)
                    CodeEditor.Output = data.Error;
                else
                    CodeEditor.Output = data.Output;
            }
            
            CodeEditor.IsLoading = false;
        }
        Task Editor_Save(bool auto = false)
        {
            ApplicationData.Current.LocalSettings.Values["Compiler_Code"] = CodeEditor.Code;
            ApplicationData.Current.LocalSettings.Values["Compiler_Input"] = CodeEditor.Input;

            if (auto == false)
            {
                NotificationQueue.Clear();
                InfoBar.Title = "Saved";
                InfoBar.Severity = muxc.InfoBarSeverity.Success;
                NotificationQueue.Show("Your code has been saved", 3000);
            }

            return Task.CompletedTask;
        }
        internal class Result
        {
            public string? Error { get; set; }
            public string? Output { get; set; }
        }
        public class Editor : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;
            public void NotifyPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            private string _Input { get; set; }
            public string Input
            {
                get
                {
                    if (string.IsNullOrEmpty(_Input))
                        return "1955";
                    else
                        return _Input.Trim();
                }
                set
                {
                    _Input = value;
                    NotifyPropertyChanged("Input");
                }
            }
            private string _Output { get; set; }
            public string Output
            {
                get => _Output;
                set
                {
                    _Output = value;
                    NotifyPropertyChanged("Output");
                }
            }
            private string _Code { get; set; }
            public string Code
            {
                get
                {
                    if (_Code == null || _Code.Trim().Length == 0)
                    {
                        var language = Variables.SelectedProgrammingLanguage;

                        if (language != null)
                            return language.DefaultCode.Replace("<#? defaultclass ?#>", "ftis");
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
            private bool _IsLoading { get; set; } = false;
            public bool IsLoading
            {
                get => _IsLoading;
                set
                {
                    _IsLoading = value;
                    NotifyPropertyChanged("IsLoading");
                }
            }
        }
    }
}