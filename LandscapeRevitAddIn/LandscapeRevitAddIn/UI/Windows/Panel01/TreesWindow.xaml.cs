using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Globalization;
using Autodesk.Revit.DB;
using LandscapeRevitAddIn.Utils;
using LandscapeRevitAddIn.Models;
using FamilyUtils = LandscapeRevitAddIn.Utils.FamilyUtils;
using WpfGrid = System.Windows.Controls.Grid;
using WpfColor = System.Windows.Media.Color;
using WpfPoint = System.Windows.Point;
using WpfFormattedText = System.Windows.Media.FormattedText;
using WpfThickness = System.Windows.Thickness;

namespace LandscapeRevitAddIn.UI.Windows.Panel01
{
    public partial class TreesWindow : Window
    {
        private Document _doc;
        private List<ImportInstance> _cadFiles;
        private List<FamilySymbol> _families;
        private Dictionary<string, List<Line>> _cadLayers;
        private List<LayerFamilyMapping> _mappings;

        public TreesWindow(Document doc)
        {
            InitializeComponent();
            _doc = doc;
            _mappings = new List<LayerFamilyMapping>();

            // Load logo
            LoadLogo();

            LoadInitialData();
        }

        private void LoadLogo()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                // List all embedded resources to see what's available
                string[] resourceNames = assembly.GetManifestResourceNames();
                System.Diagnostics.Debug.WriteLine("=== Available Embedded Resources ===");
                foreach (string name in resourceNames)
                {
                    System.Diagnostics.Debug.WriteLine(name);
                }

                // Try different file extensions
                string[] extensions = { ".png", ".jpg", ".jpeg", ".ico", ".bmp" };

                foreach (string ext in extensions)
                {
                    var resourceName = $"LandscapeRevitAddIn.Resources.Icons.XDHouse_Logo{ext}";
                    System.Diagnostics.Debug.WriteLine($"Trying to load: {resourceName}");

                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"SUCCESS: Found logo at {resourceName}");
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = stream;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze();

                            LogoBrush.ImageSource = bitmap;
                            return;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"FAILED: Could not find {resourceName}");
                        }
                    }
                }

                // If no logo found, create a simple placeholder
                System.Diagnostics.Debug.WriteLine("Creating placeholder logo");
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
                    // Create simple XDH placeholder
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

                    context.DrawText(formattedText, new WpfPoint(textX, textY));
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
                    CADFileComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = displayName,
                        Tag = cadFile
                    });
                }

                if (_cadFiles.Count == 0)
                {
                    CADInfoTextBlock.Text = "No linked CAD files found. Please link a CAD file first.";
                    CADInfoTextBlock.Foreground = Brushes.Red;
                }

                // Load families
                _families = FamilyUtils.GetPlantingFamilies(_doc);

                if (_families.Count == 0)
                {
                    MessageBox.Show("No suitable families found. Please load some planting families first.",
                        "No Families", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Error processing CAD file: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCADLayers(ImportInstance cadFile)
        {
            try
            {
                // Get layers with lines - CADUtils already returns clean layer names
                _cadLayers = CADUtils.GetLayersWithLines(cadFile);

                // No need for additional processing - the layer names are already clean!
                _mappings.Clear();
                MappingRowsPanel.Children.Clear();

                if (_cadLayers.Count > 0)
                {
                    CADInfoTextBlock.Text = $"Found {_cadLayers.Count} layers with {_cadLayers.Sum(l => l.Value.Count)} total lines";
                    CADInfoTextBlock.Foreground = Brushes.Green;

                    // Add first mapping row automatically
                    AddMappingRow();
                    AddRowButton.IsEnabled = true;
                }
                else
                {
                    CADInfoTextBlock.Text = "No layers with line geometry found in CAD file";
                    CADInfoTextBlock.Foreground = Brushes.Red;
                    AddRowButton.IsEnabled = false;

                    MappingRowsPanel.Children.Add(new TextBlock
                    {
                        Text = "No suitable layers found",
                        Foreground = Brushes.Red,
                        FontStyle = FontStyles.Italic,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new WpfThickness(20, 20, 20, 20)
                    });
                }

                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading CAD layers: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddRowButton_Click(object sender, RoutedEventArgs e)
        {
            AddMappingRow();
        }

        private void AddMappingRow()
        {
            var grid = new WpfGrid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60, GridUnitType.Pixel) });

            // Create alternating row background
            var rowIndex = MappingRowsPanel.Children.Count;
            var backgroundColor = rowIndex % 2 == 0 ? Brushes.White : new SolidColorBrush(WpfColor.FromRgb(249, 249, 249));

            // Layer dropdown with taller items
            var layerCombo = new ComboBox
            {
                Height = 40,
                Margin = new WpfThickness(1, 1, 1, 1),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(204, 204, 204)),
                BorderThickness = new WpfThickness(0, 0, 1, 1),
                Background = Brushes.White,
                FontSize = 12,
                Padding = new WpfThickness(12, 10, 12, 10)
            };

            // FIXED: Add individual layer names (not CAD files) to the dropdown
            if (_cadLayers != null && _cadLayers.Count > 0)
            {
                foreach (var layer in _cadLayers.OrderBy(l => l.Key)) // Sort alphabetically
                {
                    var item = new ComboBoxItem
                    {
                        Content = $"{layer.Key} ({layer.Value.Count} lines)",
                        Tag = layer.Key, // This is the clean layer name from CADUtils
                        Padding = new WpfThickness(12, 8, 12, 8),
                        FontSize = 12,
                        MinHeight = 32
                    };
                    layerCombo.Items.Add(item);
                }
            }
            else
            {
                // Add placeholder if no layers available
                var placeholderItem = new ComboBoxItem
                {
                    Content = "No layers available - select a CAD file first",
                    IsEnabled = false,
                    Padding = new WpfThickness(12, 8, 12, 8),
                    FontSize = 12,
                    MinHeight = 32,
                    Foreground = Brushes.Gray
                };
                layerCombo.Items.Add(placeholderItem);
            }

            layerCombo.SelectionChanged += LayerCombo_SelectionChanged;

            // Family dropdown with taller items
            var familyCombo = new ComboBox
            {
                Height = 40,
                Margin = new WpfThickness(1, 1, 1, 1),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(204, 204, 204)),
                BorderThickness = new WpfThickness(0, 0, 1, 1),
                Background = Brushes.White,
                FontSize = 12,
                Padding = new WpfThickness(12, 10, 12, 10)
            };

            // Add available families
            if (_families != null && _families.Count > 0)
            {
                foreach (var family in _families)
                {
                    var displayName = FamilyUtils.GetFamilyDisplayName(family);
                    var item = new ComboBoxItem
                    {
                        Content = displayName,
                        Tag = family,
                        Padding = new WpfThickness(12, 8, 12, 8),
                        FontSize = 12,
                        MinHeight = 32
                    };
                    familyCombo.Items.Add(item);
                }
            }
            else
            {
                // Add placeholder if no families available
                var placeholderItem = new ComboBoxItem
                {
                    Content = "No families available - load planting families first",
                    IsEnabled = false,
                    Padding = new WpfThickness(12, 8, 12, 8),
                    FontSize = 12,
                    MinHeight = 32,
                    Foreground = Brushes.Gray
                };
                familyCombo.Items.Add(placeholderItem);
            }

            familyCombo.SelectionChanged += FamilyCombo_SelectionChanged;

            // Remove button
            var removeButton = new Button
            {
                Content = "×",
                Width = 30,
                Height = 30,
                Margin = new WpfThickness(15, 5, 15, 5),
                Background = new SolidColorBrush(WpfColor.FromRgb(102, 102, 102)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(153, 153, 153)),
                BorderThickness = new WpfThickness(1, 1, 1, 1),
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Cursor = Cursors.Hand
            };

            // Add hover effect to remove button
            removeButton.MouseEnter += (s, e) => removeButton.Background = new SolidColorBrush(WpfColor.FromRgb(136, 136, 136));
            removeButton.MouseLeave += (s, e) => removeButton.Background = new SolidColorBrush(WpfColor.FromRgb(102, 102, 102));
            removeButton.Click += (s, e) => RemoveMappingRow(grid);

            // Add borders with modern styling
            var layerBorder = new Border
            {
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(204, 204, 204)),
                BorderThickness = new WpfThickness(1, 0, 0, 1),
                Child = layerCombo,
                Background = backgroundColor
            };

            var familyBorder = new Border
            {
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(204, 204, 204)),
                BorderThickness = new WpfThickness(1, 0, 0, 1),
                Child = familyCombo,
                Background = backgroundColor
            };

            var buttonBorder = new Border
            {
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(204, 204, 204)),
                BorderThickness = new WpfThickness(1, 0, 1, 1),
                Child = removeButton,
                Background = backgroundColor,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            WpfGrid.SetColumn(layerBorder, 0);
            WpfGrid.SetColumn(familyBorder, 1);
            WpfGrid.SetColumn(buttonBorder, 2);

            grid.Children.Add(layerBorder);
            grid.Children.Add(familyBorder);
            grid.Children.Add(buttonBorder);

            // Store references for easy access
            grid.Tag = new { LayerCombo = layerCombo, FamilyCombo = familyCombo };

            MappingRowsPanel.Children.Add(grid);

            // Create mapping object
            var mapping = new LayerFamilyMapping();
            _mappings.Add(mapping);

            UpdateSummary();
        }

        private void RemoveMappingRow(WpfGrid rowGrid)
        {
            var index = MappingRowsPanel.Children.IndexOf(rowGrid);
            if (index >= 0 && index < _mappings.Count)
            {
                MappingRowsPanel.Children.RemoveAt(index);
                _mappings.RemoveAt(index);
                UpdateSummary();
            }
        }

        private void LayerCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMappingFromRow(sender as ComboBox);
        }

        private void FamilyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMappingFromRow(sender as ComboBox);
        }

        private void UpdateMappingFromRow(ComboBox changedCombo)
        {
            // Find which row this combo belongs to
            for (int i = 0; i < MappingRowsPanel.Children.Count; i++)
            {
                var grid = MappingRowsPanel.Children[i] as WpfGrid;
                if (grid?.Tag != null)
                {
                    var controls = (dynamic)grid.Tag;
                    if (controls.LayerCombo == changedCombo || controls.FamilyCombo == changedCombo)
                    {
                        // Update the mapping for this row
                        if (i < _mappings.Count)
                        {
                            var layerItem = controls.LayerCombo.SelectedItem as ComboBoxItem;
                            var familyItem = controls.FamilyCombo.SelectedItem as ComboBoxItem;

                            _mappings[i].LayerName = layerItem?.Tag as string; // This is now a clean layer name
                            _mappings[i].FamilySymbol = familyItem?.Tag as FamilySymbol;

                            if (_mappings[i].LayerName != null && _cadLayers.ContainsKey(_mappings[i].LayerName))
                            {
                                _mappings[i].LineCount = _cadLayers[_mappings[i].LayerName].Count;
                            }

                            UpdateSummary();
                        }
                        break;
                    }
                }
            }
        }

        private void UpdateSummary()
        {
            var validMappings = _mappings.Where(m => m.LayerName != null && m.FamilySymbol != null).ToList();
            var totalTrees = validMappings.Sum(m => m.LineCount);

            if (validMappings.Count > 0)
            {
                SummaryTextBlock.Text = $"Ready to place {totalTrees} trees from {validMappings.Count} layer(s)";
                SummaryTextBlock.Foreground = Brushes.DarkGreen;
                PlaceTreesButton.IsEnabled = true;
            }
            else
            {
                SummaryTextBlock.Text = "Configure layer mappings to continue";
                SummaryTextBlock.Foreground = Brushes.Gray;
                PlaceTreesButton.IsEnabled = false;
            }
        }

        private void PlaceTreesButton_Click(object sender, RoutedEventArgs e)
        {
            var validMappings = _mappings.Where(m => m.LayerName != null && m.FamilySymbol != null).ToList();

            if (validMappings.Count > 0)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please configure at least one layer-to-family mapping.",
                    "No Mappings", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Property to get the valid mappings
        public List<LayerFamilyMapping> GetValidMappings()
        {
            return _mappings.Where(m => m.LayerName != null && m.FamilySymbol != null).ToList();
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