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
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using WpfColor = System.Windows.Media.Color;
using WpfFormattedText = System.Windows.Media.FormattedText;

namespace LandscapeRevitAddIn.UI.Windows.Panel04
{
    public partial class TopoMoundWindow : Window
    {
        private Document _doc;
        private TopographySurface _selectedTopography;
        private Floor _selectedFloor;
        private double _offsetValue = 0.0;

        public TopoMoundWindow(Document doc)
        {
            InitializeComponent();
            _doc = doc;

            LoadLogo();
            UpdateSummary();
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
                    var backgroundBrush = new SolidColorBrush(WpfColor.FromRgb(240, 240, 240));
                    var textBrush = new SolidColorBrush(WpfColor.FromRgb(102, 102, 102));

                    context.DrawRectangle(backgroundBrush, null, new Rect(0, 0, 72, 72));

                    var typeface = new Typeface("Arial");
                    var formattedText = new WpfFormattedText(
                        "XDH",
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        18,
                        textBrush,
                        96);

                    var textX = (72 - formattedText.Width) / 2;
                    var textY = (72 - formattedText.Height) / 2;

                    context.DrawText(formattedText, new System.Windows.Point(textX, textY));
                }

                var renderBitmap = new RenderTargetBitmap(72, 72, 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(visual);

                LogoBrush.ImageSource = renderBitmap;
            }
            catch
            {
                // If even placeholder fails, leave it empty
            }
        }

        private void SelectTopoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide();

                var uiDoc = new UIDocument(_doc);
                var selection = uiDoc.Selection;

                var topoFilter = new TopographySelectionFilter();
                var reference = selection.PickObject(ObjectType.Element, topoFilter, "Select a topography surface:");

                if (reference != null)
                {
                    var element = _doc.GetElement(reference);
                    if (element is TopographySurface topo)
                    {
                        _selectedTopography = topo;
                        SelectedTopoTextBlock.Text = "Selected: Topography Surface";
                        SelectedTopoTextBlock.Foreground = Brushes.Green;
                        UpdateSummary();
                    }
                }

                this.Show();
            }
            catch (Exception ex)
            {
                this.Show();
                if (!ex.Message.Contains("cancel"))
                {
                    MessageBox.Show($"Error selecting topography: {ex.Message}", "Selection Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SelectFloorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide();

                var uiDoc = new UIDocument(_doc);
                var selection = uiDoc.Selection;

                var floorFilter = new TopoFloorSelectionFilter(); // FIXED: Changed from FloorSelectionFilter to TopoFloorSelectionFilter
                var reference = selection.PickObject(ObjectType.Element, floorFilter, "Select a floor:");

                if (reference != null)
                {
                    var element = _doc.GetElement(reference);
                    if (element is Floor floor)
                    {
                        _selectedFloor = floor;
                        var floorTypeName = _doc.GetElement(floor.GetTypeId()).Name;
                        SelectedFloorTextBlock.Text = $"Selected: Floor (Type: {floorTypeName})";
                        SelectedFloorTextBlock.Foreground = Brushes.Green;
                        UpdateSummary();
                    }
                }

                this.Show();
            }
            catch (Exception ex)
            {
                this.Show();
                if (!ex.Message.Contains("cancel"))
                {
                    MessageBox.Show($"Error selecting floor: {ex.Message}", "Selection Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OffsetTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(OffsetTextBox.Text, out double value))
            {
                _offsetValue = value;
                OffsetTextBox.Foreground = Brushes.Black;
            }
            else
            {
                OffsetTextBox.Foreground = Brushes.Red;
            }
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            try
            {
                var summaryLines = new List<string>();

                if (_selectedTopography != null)
                {
                    summaryLines.Add("✓ Topography: Selected");
                }
                else
                {
                    summaryLines.Add("⚠ Topography: Not selected");
                }

                if (_selectedFloor != null)
                {
                    var floorTypeName = _doc.GetElement(_selectedFloor.GetTypeId()).Name;
                    summaryLines.Add($"✓ Floor: {floorTypeName}");
                }
                else
                {
                    summaryLines.Add("⚠ Floor: Not selected");
                }

                if (double.TryParse(OffsetTextBox.Text, out double offset))
                {
                    summaryLines.Add($"✓ Offset: {offset:F2} ft");
                }
                else
                {
                    summaryLines.Add("⚠ Offset: Invalid value");
                }

                summaryLines.Add("");
                summaryLines.Add("Operation: The floor will be modified to match the topography shape. The offset value will be added to the topography elevations to create the final mound shape.");

                SummaryTextBlock.Text = string.Join("\n", summaryLines);

                // Update status and enable/disable create button
                bool canCreate = _selectedTopography != null &&
                                _selectedFloor != null &&
                                double.TryParse(OffsetTextBox.Text, out double _);

                CreateMoundButton.IsEnabled = canCreate;

                if (canCreate)
                {
                    StatusTextBlock.Text = "Ready to create topo mound";
                    StatusTextBlock.Foreground = Brushes.DarkGreen;
                }
                else
                {
                    StatusTextBlock.Text = "Select topography and floor to continue";
                    StatusTextBlock.Foreground = Brushes.Gray;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating summary: {ex.Message}");
            }
        }

        private void CreateMoundButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTopography != null && _selectedFloor != null &&
                double.TryParse(OffsetTextBox.Text, out double offset))
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select both topography and floor, and enter a valid offset value.",
                    "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Properties to get the configuration
        public TopographySurface SelectedTopography => _selectedTopography;
        public Floor SelectedFloor => _selectedFloor;
        public double OffsetValue => _offsetValue;
    }

    // Selection filter for topography only
    public class TopographySelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is TopographySurface;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }

    // FIXED: Renamed from FloorSelectionFilter to TopoFloorSelectionFilter to avoid duplicates
    public class TopoFloorSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Floor;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}