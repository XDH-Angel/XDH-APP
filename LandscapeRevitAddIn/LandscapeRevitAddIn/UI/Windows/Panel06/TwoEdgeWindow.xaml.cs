// UI/Windows/Panel06/TwoEdgeAlignWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using LandscapeRevitAddIn.Commands.Panel06;

namespace LandscapeRevitAddIn.UI.Windows.Panel06
{
    public partial class TwoEdgeAlignWindow : Window
    {
        private UIDocument _uiDoc;
        private Document _doc;
        private Floor _targetFloor;
        private List<Reference> _referenceEdges;

        public TwoEdgeAlignWindow(UIDocument uiDoc)
        {
            InitializeComponent();
            _uiDoc = uiDoc;
            _doc = uiDoc.Document;
            _referenceEdges = new List<Reference>();
            LoadLogo();
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
                    var formattedText = new System.Windows.Media.FormattedText("XDH", CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight, typeface, 18, textBrush, 96);

                    var textX = (72 - formattedText.Width) / 2;
                    var textY = (72 - formattedText.Height) / 2;
                    context.DrawText(formattedText, new System.Windows.Point(textX, textY));
                }

                var renderBitmap = new RenderTargetBitmap(72, 72, 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(visual);
                LogoBrush.ImageSource = renderBitmap;
            }
            catch { }
        }

        private void SelectFloorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide();

                var floorRef = _uiDoc.Selection.PickObject(ObjectType.Element, new FloorSelectionFilter(), "Select floor to align");
                _targetFloor = _doc.GetElement(floorRef) as Floor;

                this.Show();
                UpdateUI();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                this.Show();
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Error selecting floor: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectReferenceEdgesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide();

                // Select exactly two edges
                var edgeRefs = _uiDoc.Selection.PickObjects(ObjectType.Edge, new ReferenceEdgeSelectionFilter(), "Select exactly two reference edges");

                if (edgeRefs.Count != 2)
                {
                    this.Show();
                    MessageBox.Show("Please select exactly two reference edges.", "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _referenceEdges.Clear();
                _referenceEdges.AddRange(edgeRefs);

                this.Show();
                UpdateUI();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                this.Show();
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Error selecting reference edges: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AlignButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_targetFloor == null)
                {
                    MessageBox.Show("Please select a floor first.", "No Floor Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_referenceEdges == null || _referenceEdges.Count != 2)
                {
                    MessageBox.Show("Please select exactly two reference edges.", "Invalid Edge Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                this.Hide();

                bool success = TwoEdgeAlignmentUtils.AlignFloorToTwoEdges(_doc, _targetFloor, _referenceEdges[0], _referenceEdges[1]);

                if (success)
                {
                    MessageBox.Show("Successfully aligned floor to the two reference edges.",
                        "Alignment Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    this.Show();
                    MessageBox.Show("Failed to align floor to reference edges. Please check your selections and try again.",
                        "Alignment Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                this.Show();
                MessageBox.Show($"Error during alignment: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUI()
        {
            // Update floor display
            if (_targetFloor != null)
            {
                SelectedFloorTextBlock.Text = $"Floor selected: {_targetFloor.Name}";
                SelectedFloorTextBlock.FontStyle = FontStyles.Normal;
                SelectReferenceEdgesButton.IsEnabled = true;
            }
            else
            {
                SelectedFloorTextBlock.Text = "Click to select floor to align";
                SelectedFloorTextBlock.FontStyle = FontStyles.Italic;
                SelectReferenceEdgesButton.IsEnabled = false;
            }

            // Update reference edges display
            if (_referenceEdges != null && _referenceEdges.Count == 2)
            {
                ReferenceEdgesTextBlock.Text = "Two reference edges selected";
                ReferenceEdgesTextBlock.FontStyle = FontStyles.Normal;
            }
            else if (_referenceEdges != null && _referenceEdges.Count > 0)
            {
                ReferenceEdgesTextBlock.Text = $"{_referenceEdges.Count} edge(s) selected - need exactly 2";
                ReferenceEdgesTextBlock.FontStyle = FontStyles.Italic;
            }
            else
            {
                ReferenceEdgesTextBlock.Text = _targetFloor != null ? "Click to select two reference edges" : "Select floor first, then choose two edges";
                ReferenceEdgesTextBlock.FontStyle = FontStyles.Italic;
            }

            // Update summary and enable/disable align button
            if (_targetFloor != null && _referenceEdges.Count == 2)
            {
                SummaryTextBlock.Text = "Ready to align floor to virtual plane created by two edges";
                AlignButton.IsEnabled = true;
            }
            else
            {
                SummaryTextBlock.Text = "Select floor and two reference edges to continue";
                AlignButton.IsEnabled = false;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }

    // Selection filter for floors only
    public class FloorSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Floor;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}