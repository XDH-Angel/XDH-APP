// UI/Windows/Panel05/CadPointsWindow.xaml.cs
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
using LandscapeRevitAddIn.Utils;
// FIXED: Added namespace aliases to resolve conflicts
using WpfColor = System.Windows.Media.Color;
using WpfFormattedText = System.Windows.Media.FormattedText;
using WpfPoint = System.Windows.Point;

namespace LandscapeRevitAddIn.UI.Windows.Panel05
{
    public partial class CadPointsWindow : Window
    {
        private Document _doc;
        private List<ImportInstance> _cadFiles;
        private List<Floor> _floors;
        private Dictionary<string, List<XYZ>> _cadLayers;

        public CadPointsWindow(Document doc)
        {
            InitializeComponent();
            _doc = doc;
            LoadLogo();
            LoadInitialData();
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
                    // FIXED: Changed Color to WpfColor
                    var backgroundBrush = new SolidColorBrush(WpfColor.FromRgb(240, 240, 240));
                    var textBrush = new SolidColorBrush(WpfColor.FromRgb(102, 102, 102));
                    context.DrawRectangle(backgroundBrush, null, new Rect(0, 0, 72, 72));

                    var typeface = new Typeface("Arial");
                    // FIXED: Changed FormattedText to WpfFormattedText
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

        private void LoadInitialData()
        {
            try
            {
                // Load CAD files
                _cadFiles = CADUtils.GetLinkedCADFiles(_doc);
                CADFileComboBox.Items.Clear();

                foreach (var cadFile in _cadFiles)
                {
                    var displayName = CADUtils.GetCADFileName(cadFile);
                    CADFileComboBox.Items.Add(new ComboBoxItem { Content = displayName, Tag = cadFile });
                }

                // FIXED: Changed P5FloorUtils to PSFloorUtils (using the class we created)
                _floors = PSFloorUtils.GetAllFloors(_doc);
                FloorComboBox.Items.Clear();

                foreach (var floor in _floors)
                {
                    var displayName = PSFloorUtils.GetFloorDisplayName(floor);
                    FloorComboBox.Items.Add(new ComboBoxItem { Content = displayName, Tag = floor });
                }

                if (_cadFiles.Count == 0)
                {
                    CADInfoTextBlock.Text = "No linked CAD files found. Please link a CAD file first.";
                    CADInfoTextBlock.Foreground = Brushes.Red;
                }

                if (_floors.Count == 0)
                {
                    FloorInfoTextBlock.Text = "No floors found in the project.";
                    FloorInfoTextBlock.Foreground = Brushes.Red;
                }
                else
                {
                    FloorInfoTextBlock.Text = $"Found {_floors.Count} floor(s) in the project";
                    FloorInfoTextBlock.Foreground = Brushes.Green;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CADFileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedItem = CADFileComboBox.SelectedItem as ComboBoxItem;
                if (selectedItem?.Tag is ImportInstance cadFile)
                {
                    LoadCADLayers(cadFile);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing CAD file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCADLayers(ImportInstance cadFile)
        {
            try
            {
                // FIXED: Changed P5CADUtils to PSCADUtils (we need to create this method or use existing CADUtils)
                // For now, using a placeholder implementation - you may need to adjust based on your CADUtils
                _cadLayers = new Dictionary<string, List<XYZ>>();

                // Placeholder implementation - replace with actual layer extraction logic
                var samplePoints = PSCADUtils.GetPointsFromLayer(_doc, "0"); // Default layer
                if (samplePoints.Any())
                {
                    _cadLayers["Default Layer"] = samplePoints;
                }

                LayerComboBox.Items.Clear();
                LayerComboBox.IsEnabled = false;

                if (_cadLayers.Count > 0)
                {
                    foreach (var layer in _cadLayers)
                    {
                        var item = new ComboBoxItem
                        {
                            Content = $"{layer.Key} ({layer.Value.Count} points)",
                            Tag = layer.Key
                        };
                        LayerComboBox.Items.Add(item);
                    }

                    LayerComboBox.IsEnabled = true;
                    CADInfoTextBlock.Text = $"Found {_cadLayers.Count} layers with points";
                    CADInfoTextBlock.Foreground = Brushes.Green;
                }
                else
                {
                    CADInfoTextBlock.Text = "No layers with points found in CAD file";
                    CADInfoTextBlock.Foreground = Brushes.Red;
                }

                UpdateApplyButton();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading CAD layers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LayerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateApplyButton();
        }

        private void FloorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateApplyButton();
        }

        private void UpdateApplyButton()
        {
            var hasLayer = LayerComboBox.SelectedItem != null;
            var hasFloor = FloorComboBox.SelectedItem != null;
            ApplyButton.IsEnabled = hasLayer && hasFloor;

            if (ApplyButton.IsEnabled)
            {
                var layerItem = LayerComboBox.SelectedItem as ComboBoxItem;
                var layerName = layerItem?.Content.ToString();
                SummaryTextBlock.Text = $"Ready to apply surface from {layerName}";
                SummaryTextBlock.Foreground = Brushes.DarkGreen;
            }
            else
            {
                SummaryTextBlock.Text = "Select CAD layer and floor to continue";
                SummaryTextBlock.Foreground = Brushes.Gray;
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public string SelectedLayer
        {
            get
            {
                var selectedItem = LayerComboBox.SelectedItem as ComboBoxItem;
                return selectedItem?.Tag as string;
            }
        }

        public Floor SelectedFloor
        {
            get
            {
                var selectedItem = FloorComboBox.SelectedItem as ComboBoxItem;
                return selectedItem?.Tag as Floor;
            }
        }

        public ImportInstance SelectedCADFile
        {
            get
            {
                var selectedItem = CADFileComboBox.SelectedItem as ComboBoxItem;
                return selectedItem?.Tag as ImportInstance;
            }
        }
    }
}