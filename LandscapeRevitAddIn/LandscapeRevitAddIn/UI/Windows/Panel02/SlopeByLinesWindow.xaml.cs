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
    public partial class SlopeByLinesWindow : Window
    {
        private Document _doc;
        private List<Line> _selectedLines;
        private List<Level> _allLevels;
        private Level _selectedLevel;

        public SlopeByLinesWindow(Document doc)
        {
            InitializeComponent();
            _doc = doc;
            _selectedLines = new List<Line>();

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

            // Populate level combo box
            LevelComboBox.Items.Clear();
            foreach (var level in _allLevels)
            {
                var item = new ComboBoxItem
                {
                    Content = $"{level.Name} (Elev: {level.Elevation:F2}')",
                    Tag = level
                };
                LevelComboBox.Items.Add(item);
            }

            // Set default level if available
            if (_allLevels.Count > 0)
            {
                LevelComboBox.SelectedIndex = 0;
            }
        }

        private void SelectLinesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Line selection would be implemented here
                MessageBox.Show("Line selection functionality would be implemented here.\n\n" +
                    "In the actual implementation:\n" +
                    "1. Hide this window\n" +
                    "2. Prompt user to select lines (model lines, detail lines, CAD lines, etc.)\n" +
                    "3. Store selected lines and their geometry\n" +
                    "4. Show this window again\n" +
                    "5. Update the lines status and preview",
                    "Select Lines", MessageBoxButton.OK, MessageBoxImage.Information);

                // Simulate some lines being selected for demo
                _selectedLines.Clear();
                // In real implementation, you would populate this from actual selection
                // For demo, let's pretend we selected 3 lines
                UpdateLinesStatus(3);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error selecting lines: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateLinesStatus(int lineCount = -1)
        {
            if (lineCount >= 0)
            {
                // Demo mode - simulate lines
                LinesStatusTextBlock.Text = $"{lineCount} lines selected";
                LinesStatusTextBlock.Foreground = lineCount > 0 ? Brushes.DarkGreen : Brushes.Gray;
            }
            else
            {
                // Real mode - use actual selected lines
                LinesStatusTextBlock.Text = $"{_selectedLines.Count} lines selected";
                LinesStatusTextBlock.Foreground = _selectedLines.Count > 0 ? Brushes.DarkGreen : Brushes.Gray;
            }

            UpdateSummary();
        }

        private void LevelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = LevelComboBox.SelectedItem as ComboBoxItem;
            _selectedLevel = selectedItem?.Tag as Level;
            UpdateSummary();
        }

        private void SlopePercentageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSummary();
            UpdateSlopeCalculation();
        }

        private void UpdateSlopeCalculation()
        {
            if (double.TryParse(SlopePercentageTextBox.Text, out double slope))
            {
                if (Math.Abs(slope) > 0.1)
                {
                    string direction = slope > 0 ? "upward" : "downward";
                    double ratio = Math.Abs(slope) / 100.0;
                    double riseOver = 1.0 / ratio;

                    SlopeCalculationTextBlock.Text = $"{Math.Abs(slope)}% slope = 1:{riseOver:F1} ratio ({direction})";
                    SlopeCalculationTextBlock.Foreground = Brushes.DarkBlue;
                }
                else
                {
                    SlopeCalculationTextBlock.Text = "Nearly flat surface";
                    SlopeCalculationTextBlock.Foreground = Brushes.Gray;
                }
            }
            else
            {
                SlopeCalculationTextBlock.Text = "Invalid slope value";
                SlopeCalculationTextBlock.Foreground = Brushes.Red;
            }
        }

        private void UpdateSummary()
        {
            // For demo, check if we have simulated lines (text shows count > 0)
            bool hasLines = LinesStatusTextBlock.Text.Contains("lines selected") &&
                           !LinesStatusTextBlock.Text.StartsWith("0 lines") &&
                           !LinesStatusTextBlock.Text.StartsWith("No lines");

            bool hasLevel = _selectedLevel != null;
            bool validSlope = double.TryParse(SlopePercentageTextBox.Text, out double slope);

            bool canProceed = hasLines && hasLevel && validSlope;

            if (canProceed)
            {
                int lineCount = ExtractLineCount();
                SummaryTextBlock.Text = $"Ready to create slopes for {lineCount} lines with {slope}% grade on {_selectedLevel.Name}";
                SummaryTextBlock.Foreground = Brushes.DarkGreen;
            }
            else
            {
                var missing = new List<string>();
                if (!hasLines) missing.Add("lines");
                if (!hasLevel) missing.Add("level");
                if (!validSlope) missing.Add("valid slope");

                SummaryTextBlock.Text = $"Please select: {string.Join(", ", missing)}";
                SummaryTextBlock.Foreground = Brushes.Gray;
            }

            CreateSlopeButton.IsEnabled = canProceed;
        }

        private int ExtractLineCount()
        {
            try
            {
                string text = LinesStatusTextBlock.Text;
                if (text.Contains("lines selected"))
                {
                    string countText = text.Split(' ')[0];
                    if (int.TryParse(countText, out int count))
                    {
                        return count;
                    }
                }
            }
            catch { }

            return _selectedLines.Count;
        }

        private void CreateSlopeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!LinesStatusTextBlock.Text.Contains("lines selected") ||
                LinesStatusTextBlock.Text.StartsWith("0 lines") ||
                LinesStatusTextBlock.Text.StartsWith("No lines"))
            {
                MessageBox.Show("Please select lines first.", "No Lines Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedLevel == null)
            {
                MessageBox.Show("Please select a target level.", "No Level Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(SlopePercentageTextBox.Text, out double slope))
            {
                MessageBox.Show("Please enter a valid slope percentage.", "Invalid Slope",
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

        public SlopeByLinesData GetSlopeData()
        {
            double.TryParse(SlopePercentageTextBox.Text, out double slopePercentage);

            // In real implementation, use actual selected lines
            // For demo, create dummy slope data
            var lineSlopes = _selectedLines.Select(line => new LineSlopeData
            {
                ReferenceLine = line,
                SlopePercentage = slopePercentage,
                TargetLevel = _selectedLevel
            }).ToList();

            // If no real lines (demo mode), create at least one entry for the command to process
            if (lineSlopes.Count == 0 && _selectedLevel != null)
            {
                lineSlopes.Add(new LineSlopeData
                {
                    ReferenceLine = null, // Will be handled in the command
                    SlopePercentage = slopePercentage,
                    TargetLevel = _selectedLevel
                });
            }

            return new SlopeByLinesData
            {
                LineSlopes = lineSlopes
            };
        }
    }
}