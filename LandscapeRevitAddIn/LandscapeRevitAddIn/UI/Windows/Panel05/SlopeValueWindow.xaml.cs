// UI/Windows/Panel05/SlopeValueWindow.xaml.cs
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
    public partial class SlopeValueWindow : Window
    {
        private Document _doc;
        private List<Floor> _floors;
        private XYZ _currentDirection = XYZ.BasisY; // Default North
        private double _currentSlopeValue = 2.0;
        private bool _isPercentage = true;

        public SlopeValueWindow(Document doc)
        {
            InitializeComponent();
            _doc = doc;
            LoadLogo();
            LoadInitialData();
            UpdateUI();
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
                // FIXED: Changed P5FloorUtils to PSFloorUtils
                _floors = PSFloorUtils.GetAllFloors(_doc);
                FloorComboBox.Items.Clear();

                foreach (var floor in _floors)
                {
                    var displayName = PSFloorUtils.GetFloorDisplayName(floor);
                    FloorComboBox.Items.Add(new ComboBoxItem { Content = displayName, Tag = floor });
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

                // Set default direction button style
                HighlightDirectionButton(NorthButton);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FloorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateApplyButton();
        }

        private void DirectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string direction)
            {
                // Reset all direction button styles
                ResetDirectionButtons();

                // Highlight selected button
                HighlightDirectionButton(button);

                // Set direction vector
                switch (direction)
                {
                    case "North":
                        _currentDirection = XYZ.BasisY;
                        DirectionTextBox.Text = "0";
                        break;
                    case "East":
                        _currentDirection = XYZ.BasisX;
                        DirectionTextBox.Text = "90";
                        break;
                    case "South":
                        _currentDirection = -XYZ.BasisY;
                        DirectionTextBox.Text = "180";
                        break;
                    case "West":
                        _currentDirection = -XYZ.BasisX;
                        DirectionTextBox.Text = "270";
                        break;
                }

                UpdateDirectionInfo();
                UpdateApplyButton();
            }
        }

        private void DirectionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(DirectionTextBox.Text, out double angle))
            {
                // Convert angle to direction vector
                double radians = angle * Math.PI / 180.0;
                _currentDirection = new XYZ(Math.Sin(radians), Math.Cos(radians), 0);

                // Reset direction button highlights if custom angle is used
                if (angle != 0 && angle != 90 && angle != 180 && angle != 270)
                {
                    ResetDirectionButtons();
                }

                UpdateDirectionInfo();
            }
            UpdateApplyButton();
        }

        private void SlopeValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(SlopeValueTextBox.Text, out double value) && value > 0)
            {
                _currentSlopeValue = value;
                UpdateSlopeInfo();
            }
            UpdateApplyButton();
        }

        private void UnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UnitComboBox.SelectedItem is ComboBoxItem item && item.Tag is string unit)
            {
                _isPercentage = unit == "Percentage";
                UpdateSlopeInfo();
            }
            UpdateApplyButton();
        }

        private void UpdateUI()
        {
            UpdateDirectionInfo();
            UpdateSlopeInfo();
            UpdateApplyButton();
        }

        private void UpdateDirectionInfo()
        {
            var angle = Math.Atan2(_currentDirection.X, _currentDirection.Y) * 180.0 / Math.PI;
            if (angle < 0) angle += 360;

            string directionName = GetDirectionName(angle);
            DirectionInfoTextBlock.Text = $"Current direction: {angle:F1}° ({directionName})";
            DirectionInfoTextBlock.Foreground = Brushes.DarkGreen;
        }

        private void UpdateSlopeInfo()
        {
            var unit = _isPercentage ? "%" : "°";
            SlopeInfoTextBlock.Text = $"Slope: {_currentSlopeValue}{unit}";
            SlopeInfoTextBlock.Foreground = Brushes.DarkGreen;
        }

        private void UpdateApplyButton()
        {
            var hasFloor = FloorComboBox.SelectedItem != null;
            var hasValidSlope = _currentSlopeValue > 0;

            ApplySlopeButton.IsEnabled = hasFloor && hasValidSlope;

            if (ApplySlopeButton.IsEnabled)
            {
                var unit = _isPercentage ? "%" : "°";
                SummaryTextBlock.Text = $"Ready to apply {_currentSlopeValue}{unit} slope";
                SummaryTextBlock.Foreground = Brushes.DarkGreen;
            }
            else
            {
                SummaryTextBlock.Text = "Configure floor, direction and slope value to continue";
                SummaryTextBlock.Foreground = Brushes.Gray;
            }
        }

        private void ResetDirectionButtons()
        {
            var defaultStyle = FindResource("ModernButton") as Style;
            NorthButton.Style = defaultStyle;
            EastButton.Style = defaultStyle;
            SouthButton.Style = defaultStyle;
            WestButton.Style = defaultStyle;
        }

        private void HighlightDirectionButton(Button button)
        {
            var primaryStyle = FindResource("PrimaryButton") as Style;
            button.Style = primaryStyle;
        }

        private string GetDirectionName(double angle)
        {
            if (angle >= 337.5 || angle < 22.5) return "North";
            if (angle >= 22.5 && angle < 67.5) return "Northeast";
            if (angle >= 67.5 && angle < 112.5) return "East";
            if (angle >= 112.5 && angle < 157.5) return "Southeast";
            if (angle >= 157.5 && angle < 202.5) return "South";
            if (angle >= 202.5 && angle < 247.5) return "Southwest";
            if (angle >= 247.5 && angle < 292.5) return "West";
            if (angle >= 292.5 && angle < 337.5) return "Northwest";
            return "Unknown";
        }

        private void ApplySlopeButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Public properties for accessing selected values
        public Floor SelectedFloor
        {
            get
            {
                var selectedItem = FloorComboBox.SelectedItem as ComboBoxItem;
                return selectedItem?.Tag as Floor;
            }
        }

        public XYZ SlopeDirection => _currentDirection;

        public double SlopeValue => _currentSlopeValue;

        public bool IsPercentage => _isPercentage;
    }
}