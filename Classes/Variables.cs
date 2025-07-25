using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Your_Judge.Classes
{
    public static class Variables
    {
        public static readonly string Host = "judge.agapedimas.com";
        public static string DefaultAssetsPath { get => Application.Current.RequestedTheme == ApplicationTheme.Light ? "ms-appx:///Assets/Light" : "ms-appx:///Assets/Dark"; }
        public static Uri DefaultAvatar { get => new Uri(DefaultAssetsPath + "/Avatar.png"); }
        public static string HTMLTemplate(string content, Thickness? margin = null)
        {
            if (margin == null)
                margin = new(0, 0, 0, 0);

            string overrideDark = Application.Current.RequestedTheme == ApplicationTheme.Dark ? "color: #FFFFFF; border-color: #FFFFFF !important;" : "";
            string body = "<div id='Grid_ChallengeProblem'>" + content + "</div>";
            string script =
                "<script>" +
                    "document.addEventListener(\"keydown\",e=>{if(e.ctrlKey&&[\"61\",\"107\",\"173\",\"109\",\"187\",\"189\"].includes(e.keyCode.toString()))e.preventDefault()}),document.addEventListener(\"wheel\",e=>{if(e.ctrlKey)e.preventDefault()},{passive:!1});" +
                    "window.onresize = function(){window.external.notify((Grid_ChallengeProblem.clientHeight + " + (margin.Value.Top * 2 + margin.Value.Bottom * 2) + ").toString())};" +
                    "window.onload = window.onresize;" +
                "</script>";
            string style =
                "<style>" +
                    "html,body{-ms-content-zooming:none;overflow:hidden;margin:" + margin.Value.Top + "px " + margin.Value.Right + "px " + margin.Value.Bottom + "px " + margin.Value.Left + "px " + ";" + overrideDark + "}" +
                    "#Grid_ChallengeProblem{overflow:hidden;font-family:'Segoe UI Variable Display', system-ui;font-size: 1.1rem;line-height: 1.5;}" +
                    "#Grid_ChallengeProblem *{user-select: text;max-width: 100%;white-space: normal;word-break: keep-all;" + overrideDark + "}" +
                    "#Grid_ChallengeProblem *:first-child{margin-top: 0;}" +
                    "#Grid_ChallengeProblem strong,#Grid_ChallengeProblem b{font-family: 'Segoe UI Variable Display Bold', inherit;}" +
                    "#Grid_ChallengeProblem p{margin-block-start: 0;margin-block-end: 0;margin: 0 !important}" +
                "</style>";

            return style + body + script;
        }

        private static ProgrammingLanguage? _SelectedProgrammingLanguage { get; set; }
        public static ProgrammingLanguage SelectedProgrammingLanguage
        { 
            get
            {
                if (_SelectedProgrammingLanguage == null)
                {
                    if (ApplicationData.Current.LocalSettings.Values["ProgrammingLanguage"] is not string language 
                        || 
                        ProgrammingLanguage.Available.SingleOrDefault(o => o.Alias == language) is not ProgrammingLanguage selected)
                    {
                        ApplicationData.Current.LocalSettings.Values["ProgrammingLanguage"] = "java";

                        #pragma warning disable CS8603
                        return ProgrammingLanguage.Available.SingleOrDefault(o => o.Alias == "java");
                        #pragma warning restore CS8603
                    }

                    return selected;
                }

                return _SelectedProgrammingLanguage;
            }
            set
            {
                _SelectedProgrammingLanguage = value;
                ApplicationData.Current.LocalSettings.Values["ProgrammingLanguage"] = value.Alias;
            }
        }
    }
    public class VisibilityByString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var str = value as string;
            return string.IsNullOrEmpty(str) ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
