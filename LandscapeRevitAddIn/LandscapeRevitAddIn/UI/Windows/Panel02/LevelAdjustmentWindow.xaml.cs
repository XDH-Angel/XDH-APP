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
    public partial class LevelAdjustmentWindow : Window
    {
        private Document _doc;
        private List<Element> _elements;
        private List<Level> _levels;
        private string _elementType;

        public LevelAdjustmentWindow(Document doc, List<Element> elements, string elementType)
        {
            InitializeComponent();
            _doc = doc;
            _elements = elements;
            _elementType = elementType;

            LoadLogo();
            LoadData();
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
            // Load levels
            _levels = new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            // Populate level combo box
            TargetLevelComboBox.Items.Clear();
            foreach (var level in _levels)
            {
                var item = new ComboBoxItem
                {
                    Content = $"{level.Name} (Elev: {level.Elevation:F2}')",
                    Tag = level
                };
                TargetLevelComboBox.Items.Add(item);
            }

            // Populate elements list
            var elementDisplayItems = _elements.Select(e => new ElementDisplayItem
            {
                Element = e,
                DisplayName = GetElementDisplayName(e),
                CurrentLevel = GetElementCurrentLevel(e)
            }).ToList();

            ElementsListBox.ItemsSource = elementDisplayItems;
        }

        private void UpdateUI()
        {
            TitleTextBlock.Text = $"Adjust {_elementType} Levels";
            SubtitleTextBlock.Text = $"Modify level assignments for {_elements.Count} selected {_elementType.ToLower()}";
            SummaryTextBlock.Text = $"Ready to adjust {_elements.Count} {_elementType.ToLower()}";
        }

        private string GetElementDisplayName(Element element)
        {
            string typeName = element.GetType().Name;
            string name = element.Name ?? "Unnamed";
            return $"{typeName} - {name} (ID: {element.Id})";
        }

        private string GetElementCurrentLevel(Element element)
        {
            var levelParams = new[]
            {
                BuiltInParameter.FAMILY_LEVEL_PARAM,
                BuiltInParameter.FAMILY_BASE_LEVEL_PARAM,
                BuiltInParameter.LEVEL_PARAM,
                BuiltInParameter.SCHEDULE_LEVEL_PARAM
            };

            foreach (var param in levelParams)
            {
                Parameter p = element.get_Parameter(param);
                if (p != null && p.AsElementId() != ElementId.InvalidElementId)
                {
                    Element level = _doc.GetElement(p.AsElementId());
                    return level?.Name ?? "Unknown";
                }
            }

            return "No Level";
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (TargetLevelComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a target level.", "No Level Selected",
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

        public LevelAdjustmentData GetAdjustmentData()
        {
            var selectedLevelItem = TargetLevelComboBox.SelectedItem as ComboBoxItem;
            var targetLevel = selectedLevelItem?.Tag as Level;

            double elevationOffset = 0.0;
            if (AdjustElevationCheckBox.IsChecked == true)
            {
                double.TryParse(ElevationOffsetTextBox.Text, out elevationOffset);
            }

            return new LevelAdjustmentData
            {
                TargetLevel = targetLevel,
                ElevationOffset = elevationOffset,
                AdjustElevation = AdjustElevationCheckBox.IsChecked == true
            };
        }
    }

    public class ElementDisplayItem
    {
        public Element Element { get; set; }
        public string DisplayName { get; set; }
        public string CurrentLevel { get; set; }
    }
}