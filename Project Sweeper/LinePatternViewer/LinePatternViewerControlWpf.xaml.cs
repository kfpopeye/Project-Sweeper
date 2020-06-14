using System;
using System.ComponentModel;
using System.Windows;
using WShapes = System.Windows.Shapes;
using System.Windows.Media;
using Autodesk.Revit.DB;

namespace PKHL.ProjectSweeper.LinePatternViewer
{
    /// <summary>
    /// Interaction logic for LinePatternViewerControlWpf.xaml
    /// </summary>
    public partial class LinePatternViewerControlWpf : INotifyPropertyChanged
    {
        #region LinePattern DependencyProperty
        public static readonly DependencyProperty
            LinePatternProperty = DependencyProperty
            .RegisterAttached("LinePattern",
                              typeof(LinePattern),
                              typeof(LinePatternViewerControlWpf),
                              new UIPropertyMetadata(null, OnLinePatternChanged));


        private static void OnLinePatternChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var linePatternViewerControl = d as LinePatternViewerControlWpf;
            if (linePatternViewerControl == null)
                return;
            linePatternViewerControl.CreateLinePatternOnCanvas();
            linePatternViewerControl.OnPropertyChanged("LinePattern");
        }

        public LinePattern LinePattern
        {
            get
            {
                return (LinePattern)GetValue(LinePatternProperty);
            }
            set
            {
                SetValue(LinePatternProperty, value);
            }
        }

        public LinePattern GetLinePattern(DependencyObject obj)
        {
            return (LinePattern)obj.GetValue(LinePatternProperty);
        }

        public void SetLinePattern(DependencyObject obj, LinePattern value)
        {
            obj.SetValue(LinePatternProperty, value);
        }
        #endregion

        public LinePatternViewerControlWpf()
        {
            InitializeComponent();
            theCanvas.Background = this.Background;
            this.Loaded += LinePatternViewerControlWpf_Loaded;
        }

        private void LinePatternViewerControlWpf_Loaded(object sender, RoutedEventArgs e)
        {
            CreateLinePatternOnCanvas();
        }

        public event PropertyChangedEventHandler
            PropertyChanged;

        //[NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(
            string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        

        private void CreateLinePatternOnCanvas()
        {
            theCanvas.Children.Clear();

            var width =
                (ActualWidth == 0 ? Width : ActualWidth) == 0
                ? 100
                : (ActualWidth == 0 ? Width : ActualWidth);

            if (double.IsNaN(width))
                width = 100;

            var height =
                (ActualHeight == 0 ? Height : ActualHeight) == 0
                ? 30
                : (ActualHeight == 0 ? Height : ActualHeight);

            if (double.IsNaN(height))
                height = 30;

            if (LinePattern != null && LinePattern.GetSegments().Count > 0)
            {
                System.Collections.Generic.IList<LinePatternSegment> segments = LinePattern.GetSegments();
                double x1 = 0, x2 = 0;
                while ((x2 * 96 * 12) <= width)
                {
                    foreach (LinePatternSegment lps in segments)
                    {
                        WShapes.Line l = new WShapes.Line();
                        l.StrokeThickness = 3d;
                        l.Y1 = height / 2;
                        l.Y2 = height / 2;
                        x2 += lps.Length;
                        l.StrokeEndLineCap = PenLineCap.Square;
                        l.StrokeStartLineCap = PenLineCap.Square;
                        l.X1 = x1 * 96 * 12; // 96px per inch X 12 inches per foot
                        l.X2 = x2 * 96 * 12;
                        if (l.X2 > width)
                            l.X2 = width;
                        if (l.X1 > width)
                            l.X1 = width;
                        switch (lps.Type)
                        {
                            case LinePatternSegmentType.Dash:
                                l.Stroke = Foreground;
                                break;
                            case LinePatternSegmentType.Dot:
                                l.StrokeThickness = 2.5d;
                                l.StrokeEndLineCap = PenLineCap.Round;
                                l.StrokeStartLineCap = PenLineCap.Round;
                                l.Stroke = Foreground;
                                break;
                            case LinePatternSegmentType.Space:
                                l.Stroke = Background;
                                break;
                            default:
                                throw new ArgumentException("Invalid segment type");
                        }
                        theCanvas.Children.Add(l);
                        x1 += lps.Length;
                    }
                }
            }
            else
            {
                //solid line
                WShapes.Line l = new WShapes.Line();
                l.StrokeThickness = 3d;
                l.Y1 = height / 2;
                l.Y2 = height / 2;
                l.StrokeEndLineCap = PenLineCap.Square;
                l.StrokeStartLineCap = PenLineCap.Square;
                l.X1 = 0;
                l.X2 = width;
                l.Stroke = Foreground;
                theCanvas.Children.Add(l);
            }
        }
    }
}
