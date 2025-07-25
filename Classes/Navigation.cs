using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Your_Judge.Classes
{
    public class Navigation
    {
        public static ObservableCollection<PageInfo> Pages { get; } =
        [
            new() {
                Icon = "\ue8f1",
                Title = "Challenges",
                Type = typeof(Pages.Challenges),
                Placement = PageInfo.Location.Item
            },
            new() {
                Icon = "\ue943",
                Title = "Compiler",
                Type = typeof(Pages.Compiler),
                Placement = PageInfo.Location.Item
            },
            new() {
                Icon = "\ue897",
                Title = "Help",
                Type = typeof(Pages.Help),
                Placement = PageInfo.Location.Footer
            }
        ];
    }
    public class PageInfo
    {
        public required string Icon { get; set; }
        public required Type Type { get; set; }
        public required string Title { get; set; }
        public enum Location { Item, Footer };
        public required Location Placement { get; set; }
    }
}
