using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using WpfColor = System.Windows.Media.Color;
using WpfFormattedText = System.Windows.Media.FormattedText;
using WpfPoint = System.Windows.Point;
using WpfEllipse = System.Windows.Shapes.Ellipse;
using WpfLine = System.Windows.Shapes.Line;
using WpfGrid = System.Windows.Controls.Grid;
using WpfLineSegment = System.Windows.Media.LineSegment;
using WpfPath = System.Windows.Shapes.Path;
using WpfPathGeometry = System.Windows.Media.PathGeometry;
using WpfPathFigure = System.Windows.Media.PathFigure;
using RevitPoint = Autodesk.Revit.DB.XYZ;
using RevitLine = Autodesk.Revit.DB.Line;

namespace LandscapeRevitAddIn.UI.Windows.Panel04
{
    public partial class GraphMoundWindow : Window
    {
        private Document _doc;
        private Floor _selectedFloor;
        private List<WpfPoint> _graphPoints;
        private string _direction = "X";
        private double _maxHeight = 10.0;
        private bool _isDragging = false;
        private WpfEllipse _draggedPoint = null;

        public GraphMoundWindow(Document doc)
        {
            InitializeComponent();
            _doc = doc;
            _graphPoints = new List<WpfPoint>();

            LoadLogo();
            InitializeGraph();
            UpdateStatus();
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

        private void InitializeGraph()
        {
            // Create default flat profile
            _graphPoints.Clear();
            _graphPoints.Add(new WpfPoint(0, 0.5)); // Start at middle height
            _graphPoints.Add(new WpfPoint(1, 0.5)); // End at middle height

            DrawGraph();
        }

        private void DrawGraph()
        {
            GraphCanvas.Children.Clear();

            var canvasWidth = GraphCanvas.ActualWidth > 0 ? GraphCanvas.ActualWidth : 400;
            var canvasHeight = GraphCanvas.ActualHeight > 0 ? GraphCanvas.ActualHeight : 200;

            // Draw grid
            DrawGrid(canvasWidth, canvasHeight);

            // Draw elevation profile line
            DrawProfileLine(canvasWidth, canvasHeight);

            // Draw control points
            DrawControlPoints(canvasWidth, canvasHeight);

            // Draw labels
            DrawLabels(canvasWidth, canvasHeight);
        }

        private void DrawGrid(double width, double height)
        {
            var gridBrush = new SolidColorBrush(WpfColor.FromRgb(230, 230, 230));

            // Vertical lines
            for (int i = 0; i <= 10; i++)
            {
                var x = (width - 40) * i / 10 + 20;
                var line = new WpfLine
                {
                    X1 = x,
                    Y1 = 20,
                    X2 = x,
                    Y2 = height - 20,
                    Stroke = gridBrush,
                    StrokeThickness = 0.5
                };
                GraphCanvas.Children.Add(line);
            }

            // Horizontal lines
            for (int i = 0; i <= 5; i++)
            {
                var y = (height - 40) * i / 5 + 20;
                var line = new WpfLine
                {
                    X1 = 20,
                    Y1 = y,
                    X2 = width - 20,
                    Y2 = y,
                    Stroke = gridBrush,
                    StrokeThickness = 0.5
                };
                GraphCanvas.Children.Add(line);
            }
        }

        private void DrawProfileLine(double width, double height)
        {
            if (_graphPoints.Count < 2) return;

            var pathGeometry = new WpfPathGeometry();
            var pathFigure = new WpfPathFigure();

            var sortedPoints = _graphPoints.OrderBy(p => p.X).ToList();

            // Convert first point to canvas coordinates
            var firstPoint = sortedPoints.First();
            var canvasX = 20 + (width - 40) * firstPoint.X;
            var canvasY = height - 20 - (height - 40) * firstPoint.Y;
            pathFigure.StartPoint = new WpfPoint(canvasX, canvasY);

            // Add line segments for remaining points
            foreach (var point in sortedPoints.Skip(1))
            {
                canvasX = 20 + (width - 40) * point.X;
                canvasY = height - 20 - (height - 40) * point.Y;
                pathFigure.Segments.Add(new WpfLineSegment(new WpfPoint(canvasX, canvasY), true));
            }

            pathGeometry.Figures.Add(pathFigure);

            var path = new WpfPath
            {
                Data = pathGeometry,
                Stroke = new SolidColorBrush(WpfColor.FromRgb(74, 124, 90)),
                StrokeThickness = 3,
                StrokeLineJoin = PenLineJoin.Round
            };

            GraphCanvas.Children.Add(path);
        }

        private void DrawControlPoints(double width, double height)
        {
            foreach (var point in _graphPoints)
            {
                var canvasX = 20 + (width - 40) * point.X;
                var canvasY = height - 20 - (height - 40) * point.Y;

                var ellipse = new WpfEllipse
                {
                    Width = 12,
                    Height = 12,
                    Fill = new SolidColorBrush(WpfColor.FromRgb(45, 93, 58)),
                    Stroke = Brushes.White,
                    StrokeThickness = 2,
                    Cursor = Cursors.Hand
                };

                Canvas.SetLeft(ellipse, canvasX - 6);
                Canvas.SetTop(ellipse, canvasY - 6);

                ellipse.Tag = point;
                GraphCanvas.Children.Add(ellipse);
            }
        }

        private void DrawLabels(double width, double height)
        {
            // Bottom labels (distance)
            var startLabel = new TextBlock
            {
                Text = "Start",
                FontSize = 10,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(90, 140, 106))
            };
            Canvas.SetLeft(startLabel, 20);
            Canvas.SetTop(startLabel, height - 15);
            GraphCanvas.Children.Add(startLabel);

            var endLabel = new TextBlock
            {
                Text = "End",
                FontSize = 10,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(90, 140, 106))
            };
            Canvas.SetLeft(endLabel, width - 40);
            Canvas.SetTop(endLabel, height - 15);
            GraphCanvas.Children.Add(endLabel);

            // Left labels (elevation)
            var maxLabel = new TextBlock
            {
                Text = $"{_maxHeight:F1} ft",
                FontSize = 10,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(90, 140, 106))
            };
            Canvas.SetLeft(maxLabel, 2);
            Canvas.SetTop(maxLabel, 15);
            GraphCanvas.Children.Add(maxLabel);

            var zeroLabel = new TextBlock
            {
                Text = "0 ft",
                FontSize = 10,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(90, 140, 106))
            };
            Canvas.SetLeft(zeroLabel, 2);
            Canvas.SetTop(zeroLabel, height - 30);
            GraphCanvas.Children.Add(zeroLabel);
        }

        private void SelectFloorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide();

                var uiDoc = new UIDocument(_doc);
                var selection = uiDoc.Selection;

                var floorFilter = new FloorSelectionFilter();
                var reference = selection.PickObject(ObjectType.Element, floorFilter, "Select a floor:");

                if (reference != null)
                {
                    var element = _doc.GetElement(reference);
                    if (element is Floor floor)
                    {
                        _selectedFloor = floor;
                        var floorTypeName = _doc.GetElement(floor.GetTypeId()).Name;
                        SelectedFloorTextBlock.Text = $"Selected: {floorTypeName}";
                        SelectedFloorTextBlock.Foreground = Brushes.Green;
                        UpdateStatus();
                    }
                }

                this.Show();
            }
            catch (Exception ex)
            {
                this.Show();
                if (!ex.Message.Contains("cancel"))
                {
                    MessageBox.Show($"Error selecting floor: {ex.Message}", "Selection Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DirectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = DirectionComboBox.SelectedItem as ComboBoxItem;
            _direction = selectedItem?.Tag?.ToString() ?? "X";
            UpdateStatus();
        }

        private void MaxHeightTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(MaxHeightTextBox.Text, out double value) && value > 0)
            {
                _maxHeight = value;
                MaxHeightTextBox.Foreground = Brushes.Black;
                DrawGraph(); // Redraw to update labels
            }
            else
            {
                MaxHeightTextBox.Foreground = Brushes.Red;
            }
            UpdateStatus();
        }

        private void ResetGraphButton_Click(object sender, RoutedEventArgs e)
        {
            InitializeGraph();
        }

        private void PresetHillButton_Click(object sender, RoutedEventArgs e)
        {
            _graphPoints.Clear();
            _graphPoints.Add(new WpfPoint(0, 0.1));    // Low start
            _graphPoints.Add(new WpfPoint(0.5, 0.9));  // High middle
            _graphPoints.Add(new WpfPoint(1, 0.1));    // Low end
            DrawGraph();
        }

        private void PresetValleyButton_Click(object sender, RoutedEventArgs e)
        {
            _graphPoints.Clear();
            _graphPoints.Add(new WpfPoint(0, 0.9));    // High start
            _graphPoints.Add(new WpfPoint(0.5, 0.1));  // Low middle
            _graphPoints.Add(new WpfPoint(1, 0.9));    // High end
            DrawGraph();
        }

        private void GraphCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var canvas = sender as Canvas;
            var position = e.GetPosition(canvas);

            // Check if clicking on existing point
            var hitTest = canvas.InputHitTest(position) as WpfEllipse;
            if (hitTest != null && hitTest.Tag is WpfPoint)
            {
                _isDragging = true;
                _draggedPoint = hitTest;
                canvas.CaptureMouse();
                return;
            }

            // Add new point if clicking on empty area
            var canvasWidth = canvas.ActualWidth;
            var canvasHeight = canvas.ActualHeight;

            if (position.X >= 20 && position.X <= canvasWidth - 20 &&
                position.Y >= 20 && position.Y <= canvasHeight - 20)
            {
                var normalizedX = (position.X - 20) / (canvasWidth - 40);
                var normalizedY = 1.0 - (position.Y - 20) / (canvasHeight - 40);

                normalizedX = Math.Max(0, Math.Min(1, normalizedX));
                normalizedY = Math.Max(0, Math.Min(1, normalizedY));

                _graphPoints.Add(new WpfPoint(normalizedX, normalizedY));
                DrawGraph();
            }
        }

        private void GraphCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || _draggedPoint == null) return;

            var canvas = sender as Canvas;
            var position = e.GetPosition(canvas);
            var canvasWidth = canvas.ActualWidth;
            var canvasHeight = canvas.ActualHeight;

            if (position.X >= 20 && position.X <= canvasWidth - 20 &&
                position.Y >= 20 && position.Y <= canvasHeight - 20)
            {
                var oldPoint = (WpfPoint)_draggedPoint.Tag;
                var normalizedX = (position.X - 20) / (canvasWidth - 40);
                var normalizedY = 1.0 - (position.Y - 20) / (canvasHeight - 40);

                normalizedX = Math.Max(0, Math.Min(1, normalizedX));
                normalizedY = Math.Max(0, Math.Min(1, normalizedY));

                // Update the point in the list
                var index = _graphPoints.IndexOf(oldPoint);
                if (index >= 0)
                {
                    _graphPoints[index] = new WpfPoint(normalizedX, normalizedY);
                    DrawGraph();
                }
            }
        }

        private void GraphCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _draggedPoint = null;
                var canvas = sender as Canvas;
                canvas.ReleaseMouseCapture();
            }
        }

        private void UpdateStatus()
        {
            bool canCreate = _selectedFloor != null &&
                            _graphPoints.Count >= 2 &&
                            double.TryParse(MaxHeightTextBox.Text, out double _);

            CreateMoundButton.IsEnabled = canCreate;

            if (canCreate)
            {
                StatusTextBlock.Text = "Ready to create graph mound";
                StatusTextBlock.Foreground = Brushes.DarkGreen;
            }
            else
            {
                StatusTextBlock.Text = "Select floor and configure graph to create mound";
                StatusTextBlock.Foreground = Brushes.Gray;
            }
        }

        private void CreateMoundButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFloor != null && _graphPoints.Count >= 2 &&
                double.TryParse(MaxHeightTextBox.Text, out double maxHeight))
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a floor and configure the graph profile.",
                    "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Properties to get the configuration
        public Floor SelectedFloor => _selectedFloor;
        public List<WpfPoint> GraphPoints => _graphPoints;
        public string Direction => _direction;
        public double MaxHeight => _maxHeight;
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
            return false;
        }
    }
}