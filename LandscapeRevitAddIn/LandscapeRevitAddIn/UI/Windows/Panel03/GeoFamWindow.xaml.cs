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

namespace LandscapeRevitAddIn.UI.Windows.Panel03
{
    public partial class GeoFamWindow : Window
    {
        private Document _doc;
        private List<Element> _selectedElements;
        private List<Category> _availableCategories;
        private string _familyName = "New Geometry Family";
        private Category _selectedCategory;
        private string _savePath;

        public GeoFamWindow(Document doc)
        {
            InitializeComponent();
            _doc = doc;
            _selectedElements = new List<Element>();

            LoadLogo();
            LoadCategories();
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

        private void LoadCategories()
        {
            try
            {
                // Get commonly used categories for families
                var categoryIds = new[]
                {
                    BuiltInCategory.OST_GenericModel,
                    BuiltInCategory.OST_Furniture,
                    // Removed OST_PlantingSite as it doesn't exist in older versions
                    BuiltInCategory.OST_Site,
                    BuiltInCategory.OST_LightingFixtures,
                    BuiltInCategory.OST_Entourage,
                    BuiltInCategory.OST_SpecialityEquipment
                };

                _availableCategories = new List<Category>();
                CategoryComboBox.Items.Clear();

                foreach (var categoryId in categoryIds)
                {
                    try
                    {
                        var category = Category.GetCategory(_doc, categoryId);
                        if (category != null && category.CanAddSubcategory)
                        {
                            _availableCategories.Add(category);

                            var item = new ComboBoxItem
                            {
                                Content = category.Name,
                                Tag = category
                            };
                            CategoryComboBox.Items.Add(item);
                        }
                    }
                    catch
                    {
                        // Skip categories that aren't available in this version
                        continue;
                    }
                }

                // Set default selection to Generic Model
                var genericModelItem = CategoryComboBox.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(item => ((Category)item.Tag).Name.Contains("Generic"));

                if (genericModelItem != null)
                {
                    CategoryComboBox.SelectedItem = genericModelItem;
                }
                else if (CategoryComboBox.Items.Count > 0)
                {
                    CategoryComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");

                // Add a fallback option
                CategoryComboBox.Items.Add(new ComboBoxItem
                {
                    Content = "Generic Model (Default)",
                    Tag = Category.GetCategory(_doc, BuiltInCategory.OST_GenericModel)
                });
                CategoryComboBox.SelectedIndex = 0;
            }
        }

        private void SelectElementsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide();

                var uiDoc = new UIDocument(_doc);
                var selection = uiDoc.Selection;

                var geometryFilter = new GeometrySelectionFilter();
                var references = selection.PickObjects(ObjectType.Element, geometryFilter,
                    "Select elements with geometry (loadable families, in-place families, etc.). Press ESC when done.");

                if (references != null && references.Any())
                {
                    _selectedElements.Clear();
                    foreach (var reference in references)
                    {
                        var element = _doc.GetElement(reference);
                        if (element != null)
                        {
                            _selectedElements.Add(element);
                        }
                    }

                    if (_selectedElements.Any())
                    {
                        SelectionInfoTextBlock.Text = $"Selected {_selectedElements.Count} element(s) with geometry";
                        SelectionInfoTextBlock.Foreground = Brushes.Green;
                        ClearSelectionButton.IsEnabled = true;
                    }
                    else
                    {
                        SelectionInfoTextBlock.Text = "No valid elements selected";
                        SelectionInfoTextBlock.Foreground = Brushes.Red;
                    }
                }

                this.Show();
                UpdateSummary();
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
                    MessageBox.Show($"Error selecting elements: {ex.Message}", "Selection Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedElements.Clear();
            SelectionInfoTextBlock.Text = "Click 'Select Elements' to choose geometry from loadable families or in-place families";
            SelectionInfoTextBlock.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(90, 140, 106)); // InfoText color
            ClearSelectionButton.IsEnabled = false;
            UpdateSummary();
        }

        private void FamilyNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _familyName = FamilyNameTextBox.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(_familyName))
            {
                FamilyNameTextBox.Foreground = Brushes.Red;
            }
            else if (_familyName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
            {
                FamilyNameTextBox.Foreground = Brushes.Red;
            }
            else
            {
                FamilyNameTextBox.Foreground = Brushes.Black;
            }

            UpdateSummary();
        }

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = CategoryComboBox.SelectedItem as ComboBoxItem;
            _selectedCategory = selectedItem?.Tag as Category;
            UpdateSummary();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Use OpenFileDialog instead of FolderBrowserDialog to avoid Forms dependency
                var dialog = new Microsoft.Win32.OpenFileDialog();
                dialog.Title = "Select folder location (cancel to use temp folder)";
                dialog.CheckFileExists = false;
                dialog.CheckPathExists = true;
                dialog.FileName = "Select Folder";

                if (dialog.ShowDialog() == true)
                {
                    _savePath = System.IO.Path.GetDirectoryName(dialog.FileName);
                    SavePathTextBox.Text = _savePath;
                    SavePathTextBox.Background = Brushes.White;
                    SaveInfoTextBlock.Text = $"Family will be saved to: {_savePath}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error selecting folder: {ex.Message}", "Folder Selection Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSummary()
        {
            try
            {
                var summaryLines = new List<string>();

                // Elements selection
                if (_selectedElements.Any())
                {
                    summaryLines.Add($"✓ Selected Elements: {_selectedElements.Count} element(s)");

                    var elementTypes = _selectedElements
                        .GroupBy(e => e.GetType().Name)
                        .Select(g => $"{g.Key} ({g.Count()})")
                        .ToList();

                    if (elementTypes.Count <= 3)
                    {
                        summaryLines.Add($"  Types: {string.Join(", ", elementTypes)}");
                    }
                    else
                    {
                        summaryLines.Add($"  Types: {string.Join(", ", elementTypes.Take(3))} and {elementTypes.Count - 3} more");
                    }
                }
                else
                {
                    summaryLines.Add("⚠ No elements selected");
                }

                // Family name
                if (!string.IsNullOrEmpty(_familyName) && _familyName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) < 0)
                {
                    summaryLines.Add($"✓ Family Name: {_familyName}");
                }
                else
                {
                    summaryLines.Add("⚠ Invalid or empty family name");
                }

                // Category
                if (_selectedCategory != null)
                {
                    summaryLines.Add($"✓ Category: {_selectedCategory.Name}");
                }
                else
                {
                    summaryLines.Add("⚠ No category selected");
                }

                // Save location
                if (!string.IsNullOrEmpty(_savePath))
                {
                    summaryLines.Add($"✓ Save Location: {_savePath}");
                }
                else
                {
                    summaryLines.Add("• Save Location: Temporary (will be loaded into project)");
                }

                summaryLines.Add("");
                summaryLines.Add("Operation: The selected elements' geometry will be extracted and used to create a new loadable family. The family will be automatically loaded into the current project.");

                SummaryTextBlock.Text = string.Join("\n", summaryLines);

                // Update status and enable/disable create button
                bool canCreate = _selectedElements.Any() &&
                                !string.IsNullOrEmpty(_familyName) &&
                                _familyName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) < 0 &&
                                _selectedCategory != null;

                CreateFamilyButton.IsEnabled = canCreate;

                if (canCreate)
                {
                    StatusTextBlock.Text = "Ready to create family from geometry";
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

        private void CreateFamilyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedElements.Any() && !string.IsNullOrEmpty(_familyName) && _selectedCategory != null)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please configure all settings before creating the family.",
                    "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Properties to get the configuration
        public IEnumerable<Element> SelectedElements => _selectedElements;
        public string FamilyName => _familyName;
        public Category SelectedCategory => _selectedCategory;
        public string SavePath => _savePath;
    }

    // Selection filter for elements with geometry
    public class GeometrySelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            try
            {
                if (elem == null) return false;

                // Allow family instances (loadable families)
                if (elem is FamilyInstance) return true;

                // Allow in-place families
                if (elem.Category?.Name?.Contains("Generic") == true) return true;

                // Allow other elements that might have geometry
                var geometry = elem.get_Geometry(new Options());
                if (geometry != null)
                {
                    foreach (GeometryObject geoObj in geometry)
                    {
                        if (geoObj is Solid solid && solid.Volume > 0)
                            return true;
                        if (geoObj is GeometryInstance)
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