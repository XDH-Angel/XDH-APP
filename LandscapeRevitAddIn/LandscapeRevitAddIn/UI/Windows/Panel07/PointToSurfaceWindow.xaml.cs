// UI/Windows/Panel07/PointToSurfaceWindow.xaml.cs
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
// FIXED: Added namespace aliases to resolve conflicts
using WpfColor = System.Windows.Media.Color;
using WpfFormattedText = System.Windows.Media.FormattedText;
using WpfPoint = System.Windows.Point;

namespace LandscapeRevitAddIn.UI.Windows.Panel07
{
    public partial class PointToSurfaceWindow : Window
    {
        private UIDocument _uiDoc;
        private Document _doc;
        private List<XYZ> _selectedPoints;
        private Floor _targetFloor;
        private Face _referenceSurface;

        public PointToSurfaceWindow(UIDocument uiDoc)
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
                    // FIXED: Changed to WpfColor alias for consistency
                    var backgroundBrush = new SolidColorBrush(WpfColor.FromRgb(240, 240, 240));
                    var textBrush = new SolidColorBrush(WpfColor.FromRgb(102, 102, 102));
                    context.DrawRectangle(backgroundBrush, null, new Rect(0, 0, 72, 72));

                    var typeface = new Typeface("Arial");

                    // FIXED: Use WpfFormattedText and simplify constructor
                    var formattedText = new WpfFormattedText("XDH", CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight, typeface, 18, textBrush, 96);

                    var textX = (72 - formattedText.Width) / 2;
                    var textY = (72 - formattedText.Height) / 2;
                    // FIXED: Changed Point to WpfPoint
                    context.DrawText(formattedText, new WpfPoint(textX, textY));
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
                this.Hide();

                var pointRefs = _uiDoc.Selection.PickObjects(ObjectType.PointOnElement, new FloorPointSelectionFilter(), "Select floor points to align");
                _selectedPoints.Clear();

                foreach (var pointRef in pointRefs)
                {
                    var element = _doc.GetElement(pointRef);
                    if (element is Floor floor)
                    {
                        _targetFloor = floor;
                        var point = pointRef.GlobalPoint;
                        if (point != null)
                        {
                            _selectedPoints.Add(point);
                        }
                    }
                }

                this.Show();
                UpdateUI();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                this.Show();
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Error selecting points: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectSurfaceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide();

                var surfaceRef = _uiDoc.Selection.PickObject(ObjectType.Face, new SurfaceSelectionFilter(), "Select reference surface");
                var element = _doc.GetElement(surfaceRef);
                var geometryObject = element.GetGeometryObjectFromReference(surfaceRef);
                _referenceSurface = geometryObject as Face;

                this.Show();
                UpdateUI();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                this.Show();
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Error selecting surface: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUI()
        {
            // FIXED: Use WpfColor alias for consistency in all color references
            if (_selectedPoints.Count > 0)
            {
                SelectedPointsTextBlock.Text = $"Selected {_selectedPoints.Count} point(s) to align";
                SelectedPointsTextBlock.Foreground = Brushes.DarkGreen;
                SelectSurfaceButton.IsEnabled = true;
                SelectedSurfaceTextBlock.Text = "Now select reference surface";
                SelectedSurfaceTextBlock.Foreground = new SolidColorBrush(WpfColor.FromRgb(90, 140, 106));
            }
            else
            {
                SelectedPointsTextBlock.Text = "Click to select floor points";
                SelectedPointsTextBlock.Foreground = new SolidColorBrush(WpfColor.FromRgb(90, 140, 106));
                SelectSurfaceButton.IsEnabled = false;
                SelectedSurfaceTextBlock.Text = "Select points first";
                SelectedSurfaceTextBlock.Foreground = new SolidColorBrush(WpfColor.FromRgb(90, 140, 106));
            }

            // Update surface text
            if (_referenceSurface != null)
            {
                SelectedSurfaceTextBlock.Text = "Reference surface selected";
                SelectedSurfaceTextBlock.Foreground = Brushes.DarkGreen;
            }

            // Update align button
            var canAlign = _selectedPoints.Count > 0 && _referenceSurface != null;
            AlignButton.IsEnabled = canAlign;
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
        public Face ReferenceSurface => _referenceSurface;
    }

    public class FloorPointToSurfaceSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Floor;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }

    public class SurfaceSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem != null;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return reference.ElementReferenceType == ElementReferenceType.REFERENCE_TYPE_SURFACE;
        }
    }
}