using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Windows.Controls;

namespace PKHL.ProjectSweeper
{
    /// <summary>
    /// Interaction logic for ListViewsWindow.xaml
    /// </summary>
    public partial class ListViewsWindow : Window, IDisposable
    {
        public List<string> TheData { get; set; }
        private Document theDoc = null;
        private Autodesk.Revit.ApplicationServices.Application theApp = null;
        private UIApplication theUiApp = null;
        private ViewOwnerDefinition theBSD = null;
        private PreviewControl thePreviewcontrol = null;

        public ListViewsWindow(ViewOwnerDefinition _bsd, Autodesk.Revit.ApplicationServices.Application app)
        {
            InitializeComponent();
            theBSD = _bsd;
            TheData = theBSD.OwnerViews.Values.ToList<string>();

            if (theBSD.OwnerSchedules != null)
                foreach (string s in theBSD.OwnerSchedules.Values.ToList<string>())
                    TheData.Add(">" + s);

            theApp = app;
            theUiApp = new UIApplication(app);
            theDoc = theUiApp.ActiveUIDocument.Document;						 
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Title = string.Format(LocalizationProvider.GetLocalizedValue<string>("ListViews_Title"), theBSD.StyleName); // Views using {0}
#if DEBUG
            Title = Title + " - DEBUG mode";
#endif
        }

        private void HandleDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            int view_id = -1;
            string s = (e.Source as ListViewItem).Content.ToString();
            if (s.StartsWith(">"))
            {
                //revit Taskdialog ListViews_DIA001
                // Schedule view
                // View names starting with '>' are schedules or view templates and cannot be viewed in the preview window.
                TaskDialog.Show(
                    LocalizationProvider.GetLocalizedValue<string>("ListViews_DIA001_Title"),
                    LocalizationProvider.GetLocalizedValue<string>("ListViews_DIA001_MainInst"));
            }
            else
            {
                var keysWithMatchingValues = theBSD.OwnerViews.Where(p => p.Value == s).Select(p => p.Key);
                foreach (var key in keysWithMatchingValues)
                    view_id = key;

                if (thePreviewcontrol != null)
                    thePreviewcontrol.Dispose();
                thePreviewcontrol = new PreviewControl(theDoc, new ElementId(view_id));
                _PlaceholderPanel.Children.Clear();
                _PlaceholderPanel.Children.Add(thePreviewcontrol);
            }

            //doesn't appear to work properly
            //theUiApp.ActiveUIDocument.Selection.SetElementIds(getLinesUsingGraphicStyle());
        }

        /// <summary>
        /// Gets all curve elements from a particular view with a certain graphic style
        /// </summary>
        private ICollection<ElementId> getLinesUsingGraphicStyle()
        {
            ElementClassFilter ecf = new ElementClassFilter(typeof(CurveElement));
            ParameterValueProvider pvp = new ParameterValueProvider(new ElementId((int)BuiltInParameter.BUILDING_CURVE_GSTYLE));
            FilterNumericRuleEvaluator fnre = new FilterNumericEquals();
            FilterRule IdRule = new FilterElementIdRule(pvp, fnre, new ElementId(theBSD.ItsId));
            ElementParameterFilter filter = new ElementParameterFilter(IdRule);
            LogicalAndFilter andFilter = new LogicalAndFilter(ecf, filter);

            FilteredElementCollector collector = new FilteredElementCollector(theDoc, theDoc.ActiveView.Id);
            return collector.WherePasses(andFilter).ToElementIds();
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            if (thePreviewcontrol != null)
                thePreviewcontrol.Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                thePreviewcontrol.Dispose();
                theUiApp.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
