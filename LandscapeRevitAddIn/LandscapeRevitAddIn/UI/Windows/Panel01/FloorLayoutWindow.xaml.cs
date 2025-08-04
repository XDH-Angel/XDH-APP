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
using WpfGrid = System.Windows.Controls.Grid;
using WpfColor = System.Windows.Media.Color;
using WpfPoint = System.Windows.Point;
using WpfFormattedText = System.Windows.Media.FormattedText;
using WpfThickness = System.Windows.Thickness;

namespace LandscapeRevitAddIn.UI.Windows.Panel01
{
    public partial class FloorLayoutWindow : Window
    {
        private Document _doc;
        private List<ImportInstance> _cadFiles;
        private List<FloorType> _floorTypes;
        private Dictionary<string, List<List<Curve>>> _cadLayers;
        private List<LayerFloorMapping> _mappings;

        public FloorLayoutWindow(Document doc)
        {
            InitializeComponent();
            _doc = doc;
            _mappings = new List<LayerFloorMapping>();

            // Load logo (same as Trees window)
            LoadLogo();
            LoadInitialData();
        }

        // Logo loading methods (same as Trees window)
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
                    var formattedText = new WpfFormattedText("XDH", CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight, typeface, 18, textBrush, 96);

                    var textX = (72 - formattedText.Width) / 2;
                    var textY = (72 - formattedText.Height) / 2;
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

                if (_cadFiles.Count == 0)
                {
                    CADInfoTextBlock.Text = "No linked CAD files found. Please link a CAD file first.";
                    CADInfoTextBlock.Foreground = Brushes.Red;
                }

                // Load floor types
                _floorTypes = FloorUtils.GetFloorTypes(_doc);

                if (_floorTypes.Count == 0)
                {
                    MessageBox.Show("No floor types found. Please load some floor types first.",
                        "No Floor Types", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                _cadLayers = FloorUtils.GetLayersWithClosedBoundaries(cadFile);
                _mappings.Clear();
                MappingRowsPanel.Children.Clear();

                if (_cadLayers.Count > 0)
                {
                    var totalBoundaries = _cadLayers.Sum(l => l.Value.Count);
                    CADInfoTextBlock.Text = $"Found {_cadLayers.Count} layers with {totalBoundaries} total closed boundaries";
                    CADInfoTextBlock.Foreground = Brushes.Green;

                    AddMappingRow();
                    AddRowButton.IsEnabled = true;
                }
                else
                {
                    CADInfoTextBlock.Text = "No layers with closed boundaries found in CAD file";
                    CADInfoTextBlock.Foreground = Brushes.Red;
                    AddRowButton.IsEnabled = false;

                    MappingRowsPanel.Children.Add(new TextBlock
                    {
                        Text = "No closed boundaries found",
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

            var rowIndex = MappingRowsPanel.Children.Count;
            var backgroundColor = rowIndex % 2 == 0 ? Brushes.White : new SolidColorBrush(WpfColor.FromRgb(249, 249, 249));

            // Layer dropdown
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

            foreach (var layer in _cadLayers)
            {
                var item = new ComboBoxItem
                {
                    Content = $"{layer.Key} ({layer.Value.Count} boundaries)",
                    Tag = layer.Key,
                    Padding = new WpfThickness(12, 8, 12, 8),
                    FontSize = 12,
                    MinHeight = 32
                };
                layerCombo.Items.Add(item);
            }
            layerCombo.SelectionChanged += LayerCombo_SelectionChanged;

            // Floor type dropdown
            var floorCombo = new ComboBox
            {
                Height = 40,
                Margin = new WpfThickness(1, 1, 1, 1),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(204, 204, 204)),
                BorderThickness = new WpfThickness(0, 0, 1, 1),
                Background = Brushes.White,
                FontSize = 12,
                Padding = new WpfThickness(12, 10, 12, 10)
            };

            foreach (var floorType in _floorTypes)
            {
                var item = new ComboBoxItem
                {
                    Content = FloorUtils.GetFloorTypeDisplayName(floorType),
                    Tag = floorType,
                    Padding = new WpfThickness(12, 8, 12, 8),
                    FontSize = 12,
                    MinHeight = 32
                };
                floorCombo.Items.Add(item);
            }
            floorCombo.SelectionChanged += FloorCombo_SelectionChanged;

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

            removeButton.MouseEnter += (s, e) => removeButton.Background = new SolidColorBrush(WpfColor.FromRgb(136, 136, 136));
            removeButton.MouseLeave += (s, e) => removeButton.Background = new SolidColorBrush(WpfColor.FromRgb(102, 102, 102));
            removeButton.Click += (s, e) => RemoveMappingRow(grid);

            // Borders
            var layerBorder = new Border
            {
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(204, 204, 204)),
                BorderThickness = new WpfThickness(1, 0, 0, 1),
                Child = layerCombo,
                Background = backgroundColor
            };
            var floorBorder = new Border
            {
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(204, 204, 204)),
                BorderThickness = new WpfThickness(1, 0, 0, 1),
                Child = floorCombo,
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
            WpfGrid.SetColumn(floorBorder, 1);
            WpfGrid.SetColumn(buttonBorder, 2);

            grid.Children.Add(layerBorder);
            grid.Children.Add(floorBorder);
            grid.Children.Add(buttonBorder);
            grid.Tag = new { LayerCombo = layerCombo, FloorCombo = floorCombo };

            MappingRowsPanel.Children.Add(grid);
            var mapping = new LayerFloorMapping();
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

        private void FloorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMappingFromRow(sender as ComboBox);
        }

        private void UpdateMappingFromRow(ComboBox changedCombo)
        {
            for (int i = 0; i < MappingRowsPanel.Children.Count; i++)
            {
                var grid = MappingRowsPanel.Children[i] as WpfGrid;
                if (grid?.Tag != null)
                {
                    var controls = (dynamic)grid.Tag;
                    if (controls.LayerCombo == changedCombo || controls.FloorCombo == changedCombo)
                    {
                        if (i < _mappings.Count)
                        {
                            var layerItem = controls.LayerCombo.SelectedItem as ComboBoxItem;
                            var floorItem = controls.FloorCombo.SelectedItem as ComboBoxItem;

                            _mappings[i].LayerName = layerItem?.Tag as string;
                            _mappings[i].FloorType = floorItem?.Tag as FloorType;

                            if (_mappings[i].LayerName != null && _cadLayers.ContainsKey(_mappings[i].LayerName))
                            {
                                _mappings[i].BoundaryCount = _cadLayers[_mappings[i].LayerName].Count;
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
            var validMappings = _mappings.Where(m => m.LayerName != null && m.FloorType != null).ToList();
            var totalFloors = validMappings.Sum(m => m.BoundaryCount);

            if (validMappings.Count > 0)
            {
                SummaryTextBlock.Text = $"Ready to create {totalFloors} floors from {validMappings.Count} layer(s)";
                SummaryTextBlock.Foreground = Brushes.DarkGreen;
                CreateFloorsButton.IsEnabled = true;
            }
            else
            {
                SummaryTextBlock.Text = "Configure layer mappings to continue";
                SummaryTextBlock.Foreground = Brushes.Gray;
                CreateFloorsButton.IsEnabled = false;
            }
        }

        private void CreateFloorsButton_Click(object sender, RoutedEventArgs e)
        {
            var validMappings = _mappings.Where(m => m.LayerName != null && m.FloorType != null).ToList();

            if (validMappings.Count > 0)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please configure at least one layer-to-floor-type mapping.",
                    "No Mappings", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public List<LayerFloorMapping> GetValidMappings()
        {
            return _mappings.Where(m => m.LayerName != null && m.FloorType != null).ToList();
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