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
using LandscapeRevitAddIn.Commands.Panel04;
using WpfColor = System.Windows.Media.Color;
using WpfFormattedText = System.Windows.Media.FormattedText;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfGrid = System.Windows.Controls.Grid;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace LandscapeRevitAddIn.UI.Windows.Panel04
{
    public partial class LineMoundWindow : Window
    {
        private Document _doc;
        private List<Level> _levels;
        private List<LineLevelMapping> _mappings;

        public LineMoundWindow(Document doc)
        {
            InitializeComponent();
            _doc = doc;
            _mappings = new List<LineLevelMapping>();

            LoadLogo();
            LoadLevels();
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

        private void LoadLevels()
        {
            try
            {
                _levels = new FilteredElementCollector(_doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .OrderBy(l => l.Elevation)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading levels: {ex.Message}");
                _levels = new List<Level>();
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
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80, GridUnitType.Pixel) });

            var rowIndex = MappingRowsPanel.Children.Count;
            var backgroundColor = rowIndex % 2 == 0 ? Brushes.White : new SolidColorBrush(WpfColor.FromRgb(249, 249, 249));

            // Select Lines Button
            var selectLinesButton = new Button
            {
                Content = "📏 Select Lines",
                Height = 40,
                Margin = new Thickness(1, 1, 1, 1),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(204, 204, 204)),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Background = Brushes.White,
                FontSize = 12
            };
            selectLinesButton.Click += SelectLinesButton_Click;

            // Level ComboBox
            var levelCombo = new WpfComboBox
            {
                Height = 40,
                Margin = new Thickness(1, 1, 1, 1),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(204, 204, 204)),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Background = Brushes.White,
                FontSize = 12
            };

            foreach (var level in _levels)
            {
                var item = new ComboBoxItem
                {
                    Content = level.Name,
                    Tag = level
                };
                levelCombo.Items.Add(item);
            }

            if (_levels.Any())
            {
                levelCombo.SelectedIndex = 0;
            }

            levelCombo.SelectionChanged += LevelCombo_SelectionChanged;

            // Elevation TextBox
            var elevationTextBox = new WpfTextBox
            {
                Height = 40,
                Margin = new Thickness(1, 1, 1, 1),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(204, 204, 204)),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Background = Brushes.White,
                FontSize = 12,
                Text = "10.0",
                TextAlignment = TextAlignment.Center
            };
            elevationTextBox.TextChanged += ElevationTextBox_TextChanged;

            // Remove Button
            var removeButton = new Button
            {
                Content = "×",
                Width = 30,
                Height = 30,
                Margin = new Thickness(25, 5, 25, 5),
                Background = new SolidColorBrush(WpfColor.FromRgb(166, 124, 108)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(139, 107, 91)),
                BorderThickness = new Thickness(1),
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            removeButton.Click += (s, e) => RemoveMappingRow(grid);

            // Add borders
            var selectLinesBorder = new Border
            {
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(204, 204, 204)),
                BorderThickness = new Thickness(1, 0, 0, 1),
                Child = selectLinesButton,
                Background = backgroundColor
            };

            var levelBorder = new Border
            {
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(204, 204, 204)),
                BorderThickness = new Thickness(1, 0, 0, 1),
                Child = levelCombo,
                Background = backgroundColor
            };

            var elevationBorder = new Border
            {
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(204, 204, 204)),
                BorderThickness = new Thickness(1, 0, 0, 1),
                Child = elevationTextBox,
                Background = backgroundColor
            };

            var buttonBorder = new Border
            {
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(204, 204, 204)),
                BorderThickness = new Thickness(1, 0, 1, 1),
                Child = removeButton,
                Background = backgroundColor,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            WpfGrid.SetColumn(selectLinesBorder, 0);
            WpfGrid.SetColumn(levelBorder, 1);
            WpfGrid.SetColumn(elevationBorder, 2);
            WpfGrid.SetColumn(buttonBorder, 3);

            grid.Children.Add(selectLinesBorder);
            grid.Children.Add(levelBorder);
            grid.Children.Add(elevationBorder);
            grid.Children.Add(buttonBorder);

            // Store references for easy access
            grid.Tag = new { SelectLinesButton = selectLinesButton, LevelCombo = levelCombo, ElevationTextBox = elevationTextBox };

            MappingRowsPanel.Children.Add(grid);

            // Create mapping object
            var mapping = new LineLevelMapping();
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

        private void SelectLinesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide();

                var uiDoc = new UIDocument(_doc);
                var selection = uiDoc.Selection;

                var lineFilter = new LineSelectionFilter();
                var references = selection.PickObjects(ObjectType.Element, lineFilter,
                    "Select lines for mound creation. Press ESC when done.");

                if (references != null && references.Any())
                {
                    var lines = new List<Curve>();
                    foreach (var reference in references)
                    {
                        var element = _doc.GetElement(reference);
                        if (element != null)
                        {
                            var curves = GetCurvesFromElement(element);
                            lines.AddRange(curves);
                        }
                    }

                    // Find which row this button belongs to
                    var button = sender as Button;
                    UpdateMappingForButton(button, lines);

                    if (lines.Any())
                    {
                        button.Content = $"📏 {lines.Count} Lines Selected";
                        button.Background = new SolidColorBrush(WpfColor.FromRgb(139, 195, 74));
                    }
                }

                this.Show();
                UpdateSummary();
            }
            catch (Exception ex)
            {
                this.Show();
                if (!ex.Message.Contains("cancel"))
                {
                    MessageBox.Show($"Error selecting lines: {ex.Message}", "Selection Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LevelCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMappingFromControl(sender);
        }

        private void ElevationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateMappingFromControl(sender);
        }

        private void UpdateMappingFromControl(object sender)
        {
            // Find which row this control belongs to
            for (int i = 0; i < MappingRowsPanel.Children.Count; i++)
            {
                var grid = MappingRowsPanel.Children[i] as WpfGrid;
                if (grid?.Tag != null)
                {
                    var controls = (dynamic)grid.Tag;
                    if (controls.LevelCombo == sender || controls.ElevationTextBox == sender)
                    {
                        UpdateMappingForRow(i);
                        break;
                    }
                }
            }
        }

        private void UpdateMappingForButton(Button button, List<Curve> lines)
        {
            // Find which row this button belongs to
            for (int i = 0; i < MappingRowsPanel.Children.Count; i++)
            {
                var grid = MappingRowsPanel.Children[i] as WpfGrid;
                if (grid?.Tag != null)
                {
                    var controls = (dynamic)grid.Tag;
                    if (controls.SelectLinesButton == button)
                    {
                        if (i < _mappings.Count)
                        {
                            _mappings[i].SelectedLines = lines;
                            UpdateMappingForRow(i);
                        }
                        break;
                    }
                }
            }
        }

        private void UpdateMappingForRow(int rowIndex)
        {
            if (rowIndex >= 0 && rowIndex < _mappings.Count && rowIndex < MappingRowsPanel.Children.Count)
            {
                var grid = MappingRowsPanel.Children[rowIndex] as WpfGrid;
                if (grid?.Tag != null)
                {
                    var controls = (dynamic)grid.Tag;
                    var levelItem = controls.LevelCombo.SelectedItem as ComboBoxItem;
                    var elevationText = controls.ElevationTextBox.Text;

                    _mappings[rowIndex].Level = levelItem?.Tag as Level;

                    if (double.TryParse(elevationText, out double elevation))
                    {
                        _mappings[rowIndex].Elevation = elevation;
                    }

                    var lineCount = _mappings[rowIndex].SelectedLines?.Count() ?? 0;
                    var levelName = _mappings[rowIndex].Level?.Name ?? "No Level";
                    _mappings[rowIndex].Description = $"{lineCount} lines at {levelName} ({_mappings[rowIndex].Elevation:F1} ft)";
                }
            }
            UpdateSummary();
        }

        private List<Curve> GetCurvesFromElement(Element element)
        {
            var curves = new List<Curve>();
            try
            {
                var geometry = element.get_Geometry(new Options());
                if (geometry != null)
                {
                    foreach (GeometryObject geoObj in geometry)
                    {
                        if (geoObj is Curve curve)
                        {
                            curves.Add(curve);
                        }
                        else if (geoObj is Line line)
                        {
                            curves.Add(line);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting curves: {ex.Message}");
            }
            return curves;
        }

        private void UpdateSummary()
        {
            var validMappings = _mappings.Where(m => m.SelectedLines != null && m.SelectedLines.Any() && m.Level != null).ToList();
            var totalLines = validMappings.Sum(m => m.SelectedLines?.Count() ?? 0);

            if (validMappings.Any())
            {
                SummaryTextBlock.Text = $"Ready to create {validMappings.Count} mound(s) from {totalLines} line(s)";
                SummaryTextBlock.Foreground = Brushes.DarkGreen;
                CreateMoundsButton.IsEnabled = true;
            }
            else
            {
                SummaryTextBlock.Text = "Configure line mappings to create mounds";
                SummaryTextBlock.Foreground = Brushes.Gray;
                CreateMoundsButton.IsEnabled = false;
            }
        }

        private void CreateMoundsButton_Click(object sender, RoutedEventArgs e)
        {
            var validMappings = _mappings.Where(m => m.SelectedLines != null && m.SelectedLines.Any() && m.Level != null).ToList();

            if (validMappings.Any())
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please configure at least one line-to-level mapping.",
                    "No Mappings", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Property to get the valid mappings
        public List<LineLevelMapping> GetValidMappings()
        {
            return _mappings.Where(m => m.SelectedLines != null && m.SelectedLines.Any() && m.Level != null).ToList();
        }
    }

    // Selection filter for lines
    public class LineSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            try
            {
                if (elem == null) return false;

                // Allow model lines, detail lines, and other line-based elements
                if (elem.Category?.Name?.Contains("Lines") == true) return true;
                if (elem is ModelLine || elem is DetailLine) return true;

                // Check if element has curve geometry
                var geometry = elem.get_Geometry(new Options());
                if (geometry != null)
                {
                    foreach (GeometryObject geoObj in geometry)
                    {
                        if (geoObj is Curve || geoObj is Line)
                            return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}