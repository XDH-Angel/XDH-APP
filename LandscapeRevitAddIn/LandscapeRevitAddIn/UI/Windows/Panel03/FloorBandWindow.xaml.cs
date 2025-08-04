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
using LandscapeRevitAddIn.Utils;

namespace LandscapeRevitAddIn.UI.Windows.Panel03
{
    public partial class FloorBandWindow : Window
    {
        private Document _doc;
        private Floor _selectedFloor;
        private List<FloorType> _floorTypes;
        private double _offsetDistance = 5.0;
        private bool _isOutward = true;
        private FloorType _selectedFloorType;

        public FloorBandWindow(Document doc)
        {
            InitializeComponent();
            _doc = doc;

            LoadLogo();
            LoadFloorTypes();
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
                    var backgroundBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));
                    var textBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 102, 102));

                    context.DrawRectangle(backgroundBrush, null, new Rect(0, 0, 72, 72));

                    var typeface = new Typeface("Arial");
                    var formattedText = new System.Windows.Media.FormattedText(
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

        private void LoadFloorTypes()
        {
            try
            {
                _floorTypes = new FilteredElementCollector(_doc)
                    .OfClass(typeof(FloorType))
                    .Cast<FloorType>()
                    .Where(ft => ft.IsValidObject)
                    .OrderBy(ft => ft.Name)
                    .ToList();

                FloorTypeComboBox.Items.Clear();

                foreach (var floorType in _floorTypes)
                {
                    var item = new ComboBoxItem
                    {
                        Content = floorType.Name,
                        Tag = floorType
                    };
                    FloorTypeComboBox.Items.Add(item);
                }

                if (_floorTypes.Count > 0)
                {
                    FloorTypeComboBox.SelectedIndex = 0;
                    FloorTypeInfoTextBlock.Text = $"Found {_floorTypes.Count} floor types";
                    FloorTypeInfoTextBlock.Foreground = Brushes.Green;
                }
                else
                {
                    FloorTypeInfoTextBlock.Text = "No floor types found in project";
                    FloorTypeInfoTextBlock.Foreground = Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                FloorTypeInfoTextBlock.Text = $"Error loading floor types: {ex.Message}";
                FloorTypeInfoTextBlock.Foreground = Brushes.Red;
            }
        }

        private void SelectFloorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide();

                var uiDoc = new UIDocument(_doc);
                var selection = uiDoc.Selection;

                var floorFilter = new FloorSelectionFilter();
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
            catch (Exception ex) // Changed from specific OperationCancelledException
            {
                // User cancelled selection or other error
                this.Show();
                if (ex.Message.Contains("cancel")) // Simple check for cancellation
                {
                    // User cancelled, do nothing
                }
                else
                {
                    MessageBox.Show($"Error selecting floor: {ex.Message}", "Selection Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OffsetDistanceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(OffsetDistanceTextBox.Text, out double value) && value > 0)
            {
                _offsetDistance = value;
                OffsetDistanceTextBox.Foreground = Brushes.Black;
            }
            else
            {
                OffsetDistanceTextBox.Foreground = Brushes.Red;
            }
            UpdateSummary();
        }

        private void DirectionRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            _isOutward = OutwardRadioButton.IsChecked == true;
            UpdateSummary();
        }

        private void FloorTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = FloorTypeComboBox.SelectedItem as ComboBoxItem;
            _selectedFloorType = selectedItem?.Tag as FloorType;
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            try
            {
                var summaryLines = new List<string>();

                if (_selectedFloor != null)
                {
                    var floorTypeName = _doc.GetElement(_selectedFloor.GetTypeId()).Name;
                    summaryLines.Add($"✓ Base Floor: {floorTypeName}");
                }
                else
                {
                    summaryLines.Add("⚠ No floor selected");
                }

                if (double.TryParse(OffsetDistanceTextBox.Text, out double value) && value > 0)
                {
                    summaryLines.Add($"✓ Offset Distance: {value:F2} ft");
                }
                else
                {
                    summaryLines.Add("⚠ Invalid offset distance");
                }

                summaryLines.Add($"✓ Direction: {(_isOutward ? "Outward" : "Inward")}");

                if (_selectedFloorType != null)
                {
                    summaryLines.Add($"✓ New Floor Type: {_selectedFloorType.Name}");
                }
                else
                {
                    summaryLines.Add("⚠ No floor type selected");
                }

                summaryLines.Add("");

                if (_isOutward)
                {
                    summaryLines.Add("Operation: A new floor will be created outside the existing floor boundary.");
                }
                else
                {
                    summaryLines.Add("Operation: The existing floor will be modified and a new inner floor will be created to avoid overlap.");
                }

                SummaryTextBlock.Text = string.Join("\n", summaryLines);

                // Update status and enable/disable create button
                bool canCreate = _selectedFloor != null &&
                                double.TryParse(OffsetDistanceTextBox.Text, out double dist) &&
                                dist > 0 &&
                                _selectedFloorType != null;

                CreateBandButton.IsEnabled = canCreate;

                if (canCreate)
                {
                    StatusTextBlock.Text = "Ready to create floor band";
                    StatusTextBlock.Foreground = Brushes.DarkGreen;
                }
                else
                {
                    StatusTextBlock.Text = "Configure all settings to continue";
                    StatusTextBlock.Foreground = Brushes.Gray;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating summary: {ex.Message}");
            }
        }

        private void CreateBandButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFloor != null && _selectedFloorType != null &&
                double.TryParse(OffsetDistanceTextBox.Text, out double distance) && distance > 0)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please configure all settings before creating the floor band.",
                    "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Properties to get the configuration
        public Floor SelectedFloor => _selectedFloor;
        public double OffsetDistance => _offsetDistance;
        public bool IsOutward => _isOutward;
        public FloorType SelectedFloorType => _selectedFloorType;
    }

    // Selection filter for floors only
    public class FloorSelectionFilter : ISelectionFilter
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