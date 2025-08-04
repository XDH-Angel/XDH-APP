// UI/Windows/Panel05/SurfaceWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using LandscapeRevitAddIn.Commands.Panel05;
using LandscapeRevitAddIn.Commands.Panel06;

namespace LandscapeRevitAddIn.UI.Windows.Panel05
{
    public partial class SurfaceWindow : Window
    {
        private UIDocument _uiDoc;
        private Document _doc;
        private Floor _selectedFloor;
        private Reference _referenceSurface;

        public SurfaceWindow(UIDocument uiDoc)
        {
            InitializeComponent();
            _uiDoc = uiDoc;
            _doc = uiDoc.Document;
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

        private void SelectFloorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide(); // Hide window during selection

                var floorRef = _uiDoc.Selection.PickObject(ObjectType.Element, new FloorSelectionFilter(), "Select floor to align");
                _selectedFloor = _doc.GetElement(floorRef) as Floor;

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
                MessageBox.Show($"Error selecting floor: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectSurfaceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide(); // Hide window during selection

                _referenceSurface = _uiDoc.Selection.PickObject(ObjectType.Face, new SurfaceSelectionFilter(), "Select reference surface");

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
                MessageBox.Show($"Error selecting surface: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUI()
        {
            // Update floor selection display
            if (_selectedFloor != null)
            {
                var floorType = _selectedFloor.FloorType;
                SelectedFloorTextBlock.Text = $"Selected: {floorType.Name} (ID: {_selectedFloor.Id})";
                SelectedFloorTextBlock.Foreground = Brushes.DarkGreen;
                SelectedFloorTextBlock.FontStyle = FontStyles.Normal;
                SelectSurfaceButton.IsEnabled = true;
                ReferenceSurfaceTextBlock.Text = "Now select reference surface";
                ReferenceSurfaceTextBlock.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(90, 140, 106));
            }
            else
            {
                SelectedFloorTextBlock.Text = "Click to select floor to align";
                SelectedFloorTextBlock.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(90, 140, 106));
                SelectedFloorTextBlock.FontStyle = FontStyles.Italic;
                SelectSurfaceButton.IsEnabled = false;
                ReferenceSurfaceTextBlock.Text = "Select floor first, then choose surface";
                ReferenceSurfaceTextBlock.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(90, 140, 106));
            }

            // Update surface selection display
            if (_referenceSurface != null)
            {
                var element = _doc.GetElement(_referenceSurface);
                ReferenceSurfaceTextBlock.Text = $"Reference surface selected from {element?.Name ?? "Element"}";
                ReferenceSurfaceTextBlock.Foreground = Brushes.DarkGreen;
                ReferenceSurfaceTextBlock.FontStyle = FontStyles.Normal;
            }

            // Update align button and summary
            var canAlign = _selectedFloor != null && _referenceSurface != null;
            AlignButton.IsEnabled = canAlign;

            if (canAlign)
            {
                SummaryTextBlock.Text = "Ready to align floor to reference surface";
                SummaryTextBlock.Foreground = Brushes.DarkGreen;
            }
            else
            {
                SummaryTextBlock.Text = "Select floor and reference surface to continue";
                SummaryTextBlock.Foreground = Brushes.Gray;
            }
        }

        private void AlignButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedFloor == null)
                {
                    MessageBox.Show("Please select a floor first.", "No Floor Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_referenceSurface == null)
                {
                    MessageBox.Show("Please select a reference surface.", "No Surface Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!double.TryParse(SurfaceOffsetTextBox.Text, out double offset))
                {
                    MessageBox.Show("Please enter a valid offset value.", "Invalid Offset", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                this.Hide();

                // Perform the surface alignment
                bool success = SurfaceAlignmentUtils.AlignFloorToSurface(_doc, _selectedFloor, _referenceSurface, offset);

                if (success)
                {
                    MessageBox.Show("Successfully aligned floor to reference surface.",
                        "Alignment Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    this.Show();
                    MessageBox.Show("Failed to align floor to surface. Please check your selections and try again.",
                        "Alignment Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Error during alignment: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // Public properties for accessing selected data
        public Floor SelectedFloor => _selectedFloor;
        public Reference ReferenceSurface => _referenceSurface;
        public bool ProjectFloor => ProjectFloorCheckBox.IsChecked == true;
        public bool MaintainFloorType => MaintainFloorTypeCheckBox.IsChecked == true;

        public double SurfaceOffset
        {
            get
            {
                if (double.TryParse(SurfaceOffsetTextBox.Text, out double offset))
                    return offset;
                return 0.0; // Default no offset
            }
        }
    }

    // Selection filter for floors
    public class FloorSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Floor;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false; // Only allow element selection, not references
        }
    }

    // Selection filter for surfaces
    public class SurfaceSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            // Allow elements that have surfaces (floors, walls, roofs, etc.)
            return elem is Floor ||
                   elem is Wall ||
                   elem is RoofBase ||
                   elem is FamilyInstance;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            // Allow face references for surface selection
            return reference?.GeometryObject is Face;
        }
    }
}