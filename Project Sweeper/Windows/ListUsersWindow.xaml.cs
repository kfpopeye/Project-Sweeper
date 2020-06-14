using System.Collections.Generic;
using System.Linq;
using System.Windows;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PKHL.ProjectSweeper
{
    /// <summary>
    /// Interaction logic for ListViewsWindow.xaml
    /// </summary>
    public partial class ListUsersWindow : Window
    {
        public List<AssetDefinition> TheData { get; set; }

        public ListUsersWindow(TextStyleCleaner.TextStyleDefinition _tsd)
        {
            InitializeComponent();
            Title = string.Format(LocalizationProvider.GetLocalizedValue<string>("LUW_Title_Txt"), _tsd.StyleName);
            TheData = new List<AssetDefinition>();
            foreach (string s in _tsd.OwnerSchedules.Values.ToList<string>())
                TheData.Add(new AssetDefinition(LocalizationProvider.GetLocalizedValue<string>("LUW_Asset"), s));
        }

        public ListUsersWindow(LinePatternCleaner.LinePatternDefinition _lpd)
        {
            InitializeComponent();
            Title = string.Format(LocalizationProvider.GetLocalizedValue<string>("LUW_Title_Txt"), _lpd.StyleName);
            TheData = _lpd.OwnerAssets;
        }

        public ListUsersWindow(FillPatternCleaner.FillPatternDefinition _fpd)
        {
            InitializeComponent();
            Title = string.Format(LocalizationProvider.GetLocalizedValue<string>("LUW_Title_Txt"), _fpd.StyleName);
            TheData = _fpd.getAssets();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Title = Title + " - DEBUG mode";
#endif
        }
    }
}
