// UI/Windows/Panel07/PointToEdgeWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace LandscapeRevitAddIn.UI.Windows.Panel07
{
    public partial class PointByTwoEdgesWindow : Window
    {
        private UIDocument _uiDoc;
        private Document _doc;
        private List<XYZ> _selectedPoints;
        private Floor _targetFloor;
        private Edge _referenceEdge;

        public PointByTwoEdgesWindow(UIDocument uiDoc)  // ✅ CORRECT - Constructor matches class name
        {
            InitializeComponent();
            _uiDoc = uiDoc;
            _doc = uiDoc.Document;
            _selectedPoints = new List<XYZ>();
            LoadLogo();
        }

        private void LoadLogo()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string[] extensions = { ".png", ".jpg", ".jpeg", ".ico", ".bmp" };

                foreach (string ext in extensions)
                {
                    var resourceName = $"LandscapeRevitAddIn.Resources.Icons.XDHouse_Logo{ext}";
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = stream;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze();
                            LogoBrush.ImageSource = bitmap;
                            return;
                        }
                    }
                }
                CreateLogoPlaceholder();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading logo: {ex.Message}");
                CreateLogoPlaceholder();
            }
        }

        private void CreateLogoPlaceholder()
        {
            try
            {
                var visual = new DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    var backgroundBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));
                    var textBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 102, 102));
                    context.DrawRectangle(backgroundBrush, null, new Rect(0, 0, 72, 72));

                    var typeface = new Typeface("Arial");
                    var formattedText = new System.Windows.Media.FormattedText("XDH", CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight, typeface, 18, textBrush, 96);

                    var textX = (72 - formattedText.Width) / 2;
                    var textY = (72 - formattedText.Height) / 2;
                    context.DrawText(formattedText, new System.Windows.Point(textX, textY));
                }

                var renderBitmap = new RenderTargetBitmap(72, 72, 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(visual);
                LogoBrush.ImageSource = renderBitmap;
            }
            catch { }
        }

        private void SelectPointsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide(); // Hide window during selection

                var pointRefs = _uiDoc.Selection.PickObjects(ObjectType.PointOnElement, new FloorPointSelectionFilter(), "Select floor points to align");
                _selectedPoints.Clear();

                foreach (var pointRef in pointRefs)
                {
                    var element = _doc.GetElement(pointRef);
                    if (element is Floor floor)
                    {
                        _targetFloor = floor;
                        // Get the point coordinates from the reference
                        var point = pointRef.GlobalPoint;
                        if (point != null)
                        {
                            _selectedPoints.Add(point);
                        }
                    }
                }

                this.Show(); // Show window again
                UpdateUI();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                this.Show(); // Show window again
            }
            catch (Exception ex)
            {
                this.Show(); // Show window again
                MessageBox.Show($"Error selecting points: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectReferenceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide(); // Hide window during selection

                var refEdgeRef = _uiDoc.Selection.PickObject(ObjectType.Edge, "Select reference edge");
                var element = _doc.GetElement(refEdgeRef);
                var geometryObject = element.GetGeometryObjectFromReference(refEdgeRef);
                _referenceEdge = geometryObject as Edge;

                this.Show(); // Show window again
                UpdateUI();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                this.Show(); // Show window again
            }
            catch (Exception ex)
            {
                this.Show(); // Show window again
                MessageBox.Show($"Error selecting reference edge: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUI()
        {
            // Update points text
            if (_selectedPoints.Count > 0)
            {
                SelectedPointsTextBlock.Text = $"Selected {_selectedPoints.Count} point(s) to align";
                SelectedPointsTextBlock.Foreground = Brushes.DarkGreen;
                SelectReferenceButton.IsEnabled = true;
                ReferenceEdgeTextBlock.Text = "Now select reference edge";
                ReferenceEdgeTextBlock.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(90, 140, 106));
            }
            else
            {
                SelectedPointsTextBlock.Text = "Click to select floor points to align";
                SelectedPointsTextBlock.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(90, 140, 106));
                SelectReferenceButton.IsEnabled = false;
                ReferenceEdgeTextBlock.Text = "Select points first, then choose reference";
                ReferenceEdgeTextBlock.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(90, 140, 106));
            }

            // Update reference edge text
            if (_referenceEdge != null)
            {
                ReferenceEdgeTextBlock.Text = "Reference edge selected";
                ReferenceEdgeTextBlock.Foreground = Brushes.DarkGreen;
            }

            // Update align button
            var canAlign = _selectedPoints.Count > 0 && _referenceEdge != null;
            AlignButton.IsEnabled = canAlign;

            // Update summary
            if (canAlign)
            {
                SummaryTextBlock.Text = $"Ready to align {_selectedPoints.Count} point(s) to reference";
                SummaryTextBlock.Foreground = Brushes.DarkGreen;
            }
            else
            {
                SummaryTextBlock.Text = "Select floor points and reference edge to continue";
                SummaryTextBlock.Foreground = Brushes.Gray;
            }
        }

        private void AlignButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Public properties for accessing selected data
        public List<XYZ> SelectedPoints => _selectedPoints;
        public Floor TargetFloor => _targetFloor;
        public Edge ReferenceEdge => _referenceEdge;

        public bool ProjectPoints => ProjectPointsCheckBox.IsChecked == true;
        public bool UpdateFloorSketch => UpdateFloorSketchCheckBox.IsChecked == true;

        public double PointTolerance
        {
            get
            {
                if (double.TryParse(PointToleranceTextBox.Text, out double tolerance))
                    return tolerance;
                return 0.01; // Default tolerance
            }
        }
    }

    public class FloorPointByTwoEdgesSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Floor;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true; // Allow point selection on floors
        }
    }
}