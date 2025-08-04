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
using WpfColor = System.Windows.Media.Color;
using WpfFormattedText = System.Windows.Media.FormattedText;

namespace LandscapeRevitAddIn.UI.Windows.Panel03
{
    public partial class VisRefWindow : Window
    {
        private Document _doc;
        private List<Edge> _selectedEdges;
        private List<Material> _materials;
        private string _surfaceType = "Loft";
        private ElementId _selectedMaterialId = ElementId.InvalidElementId;

        public VisRefWindow(Document doc)
        {
            InitializeComponent();
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
            _selectedEdges = new List<Edge>();

            LoadLogo();
            LoadMaterials();
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
                // Leave blank
            }
        }

        private void LoadMaterials()
        {
            try
            {
                _materials = new FilteredElementCollector(_doc)
                    .OfClass(typeof(Material))
                    .Cast<Material>()
                    .Where(m => m.IsValidObject)
                    .OrderBy(m => m.Name)
                    .ToList();

                MaterialComboBox.Items.Clear();

                var defaultItem = new ComboBoxItem
                {
                    Content = "No Material (Default)",
                    Tag = ElementId.InvalidElementId
                };
                MaterialComboBox.Items.Add(defaultItem);

                foreach (var material in _materials)
                {
                    var item = new ComboBoxItem
                    {
                        Content = material.Name,
                        Tag = material.Id
                    };
                    MaterialComboBox.Items.Add(item);
                }

                MaterialComboBox.SelectedIndex = 0;
                MaterialInfoTextBlock.Text = $"Found {_materials.Count} materials in project";
                MaterialInfoTextBlock.Foreground = Brushes.Green;
            }
            catch (Exception ex)
            {
                MaterialInfoTextBlock.Text = $"Error loading materials: {ex.Message}";
                MaterialInfoTextBlock.Foreground = Brushes.Red;
            }
        }

        private void SelectEdgesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Visibility = System.Windows.Visibility.Hidden;

                var uiDoc = new UIDocument(_doc);
                var selection = uiDoc.Selection;

                var edgeFilter = new EdgeSelectionFilter();
                var references = selection.PickObjects(ObjectType.Edge, edgeFilter,
                    "Select edges to create reference surface. Press ESC when done.");

                if (references != null && references.Any())
                {
                    _selectedEdges.Clear();
                    foreach (var reference in references)
                    {
                        var element = _doc.GetElement(reference);
                        if (element != null)
                        {
                            var geomObj = element.GetGeometryObjectFromReference(reference);
                            if (geomObj is Edge edge)
                            {
                                _selectedEdges.Add(edge);
                            }
                        }
                    }

                    if (_selectedEdges.Any())
                    {
                        EdgeSelectionInfoTextBlock.Text = $"Selected {_selectedEdges.Count} edge(s)";
                        EdgeSelectionInfoTextBlock.Foreground = Brushes.Green;
                        ClearEdgesButton.IsEnabled = true;
                    }
                    else
                    {
                        EdgeSelectionInfoTextBlock.Text = "No valid edges selected";
                        EdgeSelectionInfoTextBlock.Foreground = Brushes.Red;
                    }
                }

                this.Visibility = System.Windows.Visibility.Visible;
                UpdateSummary();
            }
            catch (Exception ex) // Changed to catch all exceptions to avoid version-specific issues
            {
                this.Visibility = System.Windows.Visibility.Visible;
                if (ex.Message.Contains("cancel") || ex.Message.Contains("Cancel"))
                {
                    // User cancelled, do nothing
                }
                else
                {
                    MessageBox.Show($"Error selecting edges: {ex.Message}", "Selection Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearEdgesButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedEdges.Clear();
            EdgeSelectionInfoTextBlock.Text = "Click 'Select Edges' to choose edges for reference surface";
            EdgeSelectionInfoTextBlock.Foreground = new SolidColorBrush(WpfColor.FromRgb(90, 140, 106));
            ClearEdgesButton.IsEnabled = false;
            UpdateSummary();
        }

        private void SurfaceTypeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (LoftRadioButton.IsChecked == true)
                _surfaceType = "Loft";
            else if (RuledRadioButton.IsChecked == true)
                _surfaceType = "Ruled";
            else if (PlanarRadioButton.IsChecked == true)
                _surfaceType = "Planar";

            UpdateSummary();
        }

        private void MaterialComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = MaterialComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null && selectedItem.Tag is ElementId id)
                _selectedMaterialId = id;
            else
                _selectedMaterialId = ElementId.InvalidElementId;

            UpdateSummary();
        }

        private void UpdateSummary()
        {
            try
            {
                var summaryLines = new List<string>();

                if (_selectedEdges.Any())
                {
                    summaryLines.Add($"✓ Selected Edges: {_selectedEdges.Count}");
                }
                else
                {
                    summaryLines.Add("⚠ No edges selected");
                }

                summaryLines.Add($"✓ Surface Type: {_surfaceType}");

                switch (_surfaceType)
                {
                    case "Loft":
                        summaryLines.Add("  Creates smooth transitions between edge curves");
                        break;
                    case "Ruled":
                        summaryLines.Add("  Creates linear surfaces between edge pairs");
                        break;
                    case "Planar":
                        summaryLines.Add("  Creates flat surface from edge boundary");
                        break;
                }

                if (_selectedMaterialId != ElementId.InvalidElementId)
                {
                    var material = _doc.GetElement(_selectedMaterialId) as Material;
                    summaryLines.Add($"✓ Material: {material?.Name}");
                }
                else
                {
                    summaryLines.Add("• Material: Default (no material applied)");
                }

                summaryLines.Add("");
                summaryLines.Add("Operation: The selected edges will be used to generate a visual reference surface.");

                SummaryTextBlock.Text = string.Join("\n", summaryLines);

                CreateVisRefButton.IsEnabled = _selectedEdges.Any();

                StatusTextBlock.Text = _selectedEdges.Any()
                    ? "Ready to create visual reference"
                    : "Select edges to create visual reference";
                StatusTextBlock.Foreground = _selectedEdges.Any() ? Brushes.DarkGreen : Brushes.Gray;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating summary: {ex.Message}");
            }
        }

        private void CreateVisRefButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEdges.Any())
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select edges before creating the visual reference.",
                    "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public IEnumerable<Edge> SelectedEdges => _selectedEdges;
        public string SurfaceType => _surfaceType;
        public ElementId SelectedMaterialId => _selectedMaterialId;
    }

    public class EdgeSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem) => elem != null;

        public bool AllowReference(Reference reference, XYZ position)
        {
            try
            {
                return reference.ElementReferenceType == ElementReferenceType.REFERENCE_TYPE_LINEAR;
            }
            catch
            {
                return false;
            }
        }
    }
}