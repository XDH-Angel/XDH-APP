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
using LandscapeRevitAddIn.Models;
using WpfColor = System.Windows.Media.Color;
using WpfPoint = System.Windows.Point;
using WpfFormattedText = System.Windows.Media.FormattedText;

namespace LandscapeRevitAddIn.UI.Windows.Panel02
{
    public partial class MultipleLinesLevelsWindow : Window
    {
        private Document _doc;
        private List<Floor> _selectedFloors;
        private List<Line> _selectedLines;
        private List<Level> _selectedLevels;
        private List<Level> _allLevels;

        public MultipleLinesLevelsWindow(Document doc)
        {
            InitializeComponent();
            _doc = doc;
            _selectedFloors = new List<Floor>();
            _selectedLines = new List<Line>();
            _selectedLevels = new List<Level>();

            LoadLogo();
            LoadData();
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
            catch
            {
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

                    context.DrawRectangle(backgroundBrush, null, new Rect(0, 0, 60, 60));

                    var typeface = new Typeface("Arial");
                    var formattedText = new WpfFormattedText(
                        "XDH",
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        16,
                        textBrush,
                        96);

                    var textX = (60 - formattedText.Width) / 2;
                    var textY = (60 - formattedText.Height) / 2;

                    context.DrawText(formattedText, new WpfPoint(textX, textY));
                }

                var renderBitmap = new RenderTargetBitmap(60, 60, 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(visual);

                LogoBrush.ImageSource = renderBitmap;
            }
            catch
            {
                // If even placeholder fails, leave it empty
            }
        }

        private void LoadData()
        {
            // Load all levels
            _allLevels = new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            PopulateLevelsListBox();
        }

        private void PopulateLevelsListBox()
        {
            LevelsListBox.Items.Clear();
            foreach (var level in _allLevels)
            {
                var checkBox = new CheckBox
                {
                    Content = $"{level.Name} (Elev: {level.Elevation:F2}')",
                    Tag = level,
                    Margin = new Thickness(5)
                };
                checkBox.Checked += LevelCheckBox_Changed;
                checkBox.Unchecked += LevelCheckBox_Changed;
                LevelsListBox.Items.Add(checkBox);
            }
        }

        private void SelectFloorsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("Floor selection functionality would be implemented here.\n\n" +
                    "In the actual implementation:\n" +
                    "1. Hide this window\n" +
                    "2. Prompt user to select floors\n" +
                    "3. Store selected floors\n" +
                    "4. Show this window again\n" +
                    "5. Update the floors list", 
                    "Select Floors", MessageBoxButton.OK, MessageBoxImage.Information);
                
                _selectedFloors.Clear();
                UpdateSelectionStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error selecting floors: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectLinesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("Line selection functionality would be implemented here.\n\n" +
                    "In the actual implementation:\n" +
                    "1. Hide this window\n" +
                    "2. Prompt user to select lines (model lines, detail lines, etc.)\n" +
                    "3. Store selected lines\n" +
                    "4. Show this window again\n" +
                    "5. Update the lines list", 
                    "Select Lines", MessageBoxButton.OK, MessageBoxImage.Information);
                
                _selectedLines.Clear();
                UpdateSelectionStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error selecting lines: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LevelCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateSelectedLevels();
            UpdateSummary();
        }

        private void UpdateSelectedLevels()
        {
            _selectedLevels.Clear();
            foreach (CheckBox checkBox in LevelsListBox.Items.OfType<CheckBox>())
            {
                if (checkBox.IsChecked == true && checkBox.Tag is Level level)
                {
                    _selectedLevels.Add(level);
                }
            }
        }

        private void UpdateSelectionStatus()
        {
            SelectionStatusTextBlock.Text = $"Floors: {_selectedFloors.Count}, Lines: {_selectedLines.Count}";
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            var summary = $"Floors: {_selectedFloors.Count}, Lines: {_selectedLines.Count}, Levels: {_selectedLevels.Count}";
            SummaryTextBlock.Text = summary;

            bool canProceed = _selectedFloors.Count > 0 && _selectedLines.Count > 0 && _selectedLevels.Count > 0;
            ApplyButton.IsEnabled = canProceed;

            if (canProceed)
            {
                SummaryTextBlock.Foreground = Brushes.DarkGreen;
                SummaryTextBlock.Text = $"Ready to modify {_selectedFloors.Count} floors using {_selectedLines.Count} lines and {_selectedLevels.Count} levels";
            }
            else
            {
                SummaryTextBlock.Foreground = Brushes.Gray;
                SummaryTextBlock.Text = "Select floors, lines, and levels to continue";
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFloors.Count == 0 || _selectedLines.Count == 0 || _selectedLevels.Count == 0)
            {
                MessageBox.Show("Please select floors, lines, and levels.", "Incomplete Selection", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public MultipleLinesLevelsData GetAdjustmentData()
        {
            var floorAdjustments = _selectedFloors.Select(floor => new FloorLinesLevelsData
            {
                Floor = floor,
                ReferenceLines = new List<Line>(_selectedLines),
                ReferenceLevels = new List<Level>(_selectedLevels)
            }).ToList();

            return new MultipleLinesLevelsData
            {
                FloorAdjustments = floorAdjustments
            };
        }
    }
}