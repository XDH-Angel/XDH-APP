// UI/Windows/Panel06/OneEdgeWindow.xaml.cs
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
// FIXED: Added namespace aliases to resolve conflicts
using WpfColor = System.Windows.Media.Color;
using WpfFormattedText = System.Windows.Media.FormattedText;
using WpfPoint = System.Windows.Point;

namespace LandscapeRevitAddIn.UI.Windows.Panel06
{
    public partial class OneEdgeWindow : Window
    {
        private UIDocument _uiDoc;
        private Document _doc;
        private List<Reference> _selectedFloorEdges;
        private Reference _referenceEdge;

        public OneEdgeWindow(UIDocument uiDoc)
        {
            InitializeComponent();
            _uiDoc = uiDoc;
            _doc = uiDoc.Document;
            _selectedFloorEdges = new List<Reference>();
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
                    // FIXED: Changed to WpfColor alias for consistency
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

        private void SelectFloorEdgesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide();

                // Using YOUR existing FloorEdgeSelectionFilter class
                var edgeRefs = _uiDoc.Selection.PickObjects(ObjectType.Edge, new FloorEdgeSelectionFilter(), "Select floor edges to align");
                _selectedFloorEdges.Clear();
                _selectedFloorEdges.AddRange(edgeRefs);

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
                MessageBox.Show($"Error selecting floor edges: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectReferenceEdgeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide();

                // Using YOUR existing ReferenceEdgeSelectionFilter class
                _referenceEdge = _uiDoc.Selection.PickObject(ObjectType.Edge, new ReferenceEdgeSelectionFilter(), "Select reference edge");

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
                MessageBox.Show($"Error selecting reference edge: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AlignButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedFloorEdges == null || _selectedFloorEdges.Count == 0)
                {
                    MessageBox.Show("Please select floor edges first.", "No Edges Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_referenceEdge == null)
                {
                    MessageBox.Show("Please select a reference edge.", "No Reference Edge Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!double.TryParse(ToleranceTextBox.Text, out double tolerance) || tolerance <= 0)
                {
                    MessageBox.Show("Please enter a valid tolerance value.", "Invalid Tolerance", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                this.Hide();

                // Using YOUR existing EdgeAlignmentUtils class
                bool success = EdgeAlignmentUtils.AlignFloorEdgesToReference(_doc, _selectedFloorEdges, _referenceEdge);

                if (success)
                {
                    MessageBox.Show($"Successfully aligned {_selectedFloorEdges.Count} floor edges to the reference edge.",
                        "Alignment Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    this.Show();
                    MessageBox.Show("Failed to align floor edges. Please check your selections and try again.",
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
            // FIXED: Use WpfColor alias for consistency in all color references
            if (_selectedFloorEdges != null && _selectedFloorEdges.Count > 0)
            {
                SelectedEdgesTextBlock.Text = $"{_selectedFloorEdges.Count} floor edges selected";
                SelectedEdgesTextBlock.FontStyle = FontStyles.Normal;
                SelectedEdgesTextBlock.Foreground = Brushes.DarkGreen;
                SelectReferenceEdgeButton.IsEnabled = true;
            }
            else
            {
                SelectedEdgesTextBlock.Text = "Click to select floor edges to align";
                SelectedEdgesTextBlock.FontStyle = FontStyles.Italic;
                SelectedEdgesTextBlock.Foreground = new SolidColorBrush(WpfColor.FromRgb(90, 140, 106));
                SelectReferenceEdgeButton.IsEnabled = false;
            }

            // Update reference edge display
            if (_referenceEdge != null)
            {
                var element = _doc.GetElement(_referenceEdge);
                ReferenceEdgeTextBlock.Text = $"Reference edge selected from {element?.Name ?? "Element"}";
                ReferenceEdgeTextBlock.FontStyle = FontStyles.Normal;
                ReferenceEdgeTextBlock.Foreground = Brushes.DarkGreen;
            }
            else
            {
                ReferenceEdgeTextBlock.Text = _selectedFloorEdges.Count > 0 ? "Click to select reference edge" : "Select floor edges first, then choose reference";
                ReferenceEdgeTextBlock.FontStyle = FontStyles.Italic;
                ReferenceEdgeTextBlock.Foreground = new SolidColorBrush(WpfColor.FromRgb(90, 140, 106));
            }

            // Update summary and enable/disable align button
            if (_selectedFloorEdges.Count > 0 && _referenceEdge != null)
            {
                SummaryTextBlock.Text = $"Ready to align {_selectedFloorEdges.Count} edges to reference edge";
                SummaryTextBlock.Foreground = Brushes.DarkGreen;
                AlignButton.IsEnabled = true;
            }
            else
            {
                SummaryTextBlock.Text = "Select floor edges and reference edge to continue";
                SummaryTextBlock.Foreground = Brushes.Gray;
                AlignButton.IsEnabled = false;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}