using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using LandscapeRevitAddIn.UI.Windows.Panel04;

namespace LandscapeRevitAddIn.Commands.Panel04
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class GraphMoundCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                if (doc == null)
                {
                    TaskDialog.Show("Error", "No active document found.");
                    return Result.Failed;
                }

                // Show the GraphMound window
                var graphMoundWindow = new GraphMoundWindow(doc);
                var dialogResult = graphMoundWindow.ShowDialog();

                if (dialogResult == true)
                {
                    var selectedFloor = graphMoundWindow.SelectedFloor;
                    var graphPoints = graphMoundWindow.GraphPoints;
                    var direction = graphMoundWindow.Direction;
                    var maxHeight = graphMoundWindow.MaxHeight;

                    if (selectedFloor != null && graphPoints != null && graphPoints.Any())
                    {
                        var result = CreateGraphMound(doc, selectedFloor, graphPoints, direction, maxHeight);

                        if (result)
                        {
                            TaskDialog.Show("Success",
                                $"Graph mound created successfully with {graphPoints.Count} profile points.");
                        }
                        else
                        {
                            TaskDialog.Show("Error", "Failed to create graph mound. Please check the configuration and try again.");
                        }
                    }
                    else
                    {
                        TaskDialog.Show("No Configuration", "Please select a floor and configure the graph profile.");
                    }
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Error", $"An error occurred: {ex.Message}");
                return Result.Failed;
            }
        }

        private bool CreateGraphMound(Document doc, Floor floor, List<System.Windows.Point> graphPoints,
            string direction, double maxHeight)
        {
            try
            {
                using (Transaction trans = new Transaction(doc, "Create Graph Mound"))
                {
                    trans.Start();

                    // Get floor boundary
                    var floorBoundary = GetFloorBoundary(floor);
                    if (!floorBoundary.Any())
                    {
                        trans.RollBack();
                        return false;
                    }

                    // Calculate floor center and dimensions
                    var center = GetFloorCenter(floorBoundary);
                    var dimensions = GetFloorDimensions(floorBoundary);

                    // Generate topographic points based on the graph profile
                    var topoPoints = GenerateTopoPointsFromGraph(floorBoundary, graphPoints,
                        direction, maxHeight, center, dimensions);

                    if (topoPoints.Count >= 3)
                    {
                        // Create topography surface
                        TopographySurface.Create(doc, topoPoints);
                    }

                    trans.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating graph mound: {ex.Message}");
                return false;
            }
        }

        private List<XYZ> GetFloorBoundary(Floor floor)
        {
            var points = new List<XYZ>();
            try
            {
                var sketchId = floor.SketchId;
                if (sketchId != ElementId.InvalidElementId)
                {
                    var sketch = floor.Document.GetElement(sketchId) as Sketch;
                    if (sketch != null)
                    {
                        foreach (CurveArray curveArray in sketch.Profile)
                        {
                            foreach (Curve curve in curveArray)
                            {
                                points.Add(curve.GetEndPoint(0));
                                points.Add(curve.GetEndPoint(1));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting floor boundary: {ex.Message}");
            }

            return points.Distinct().ToList();
        }

        private XYZ GetFloorCenter(List<XYZ> boundaryPoints)
        {
            if (!boundaryPoints.Any()) return XYZ.Zero;

            var avgX = boundaryPoints.Average(p => p.X);
            var avgY = boundaryPoints.Average(p => p.Y);
            var avgZ = boundaryPoints.Average(p => p.Z);

            return new XYZ(avgX, avgY, avgZ);
        }

        private (double Width, double Length) GetFloorDimensions(List<XYZ> boundaryPoints)
        {
            if (!boundaryPoints.Any()) return (0, 0);

            var minX = boundaryPoints.Min(p => p.X);
            var maxX = boundaryPoints.Max(p => p.X);
            var minY = boundaryPoints.Min(p => p.Y);
            var maxY = boundaryPoints.Max(p => p.Y);

            return (maxX - minX, maxY - minY);
        }

        private List<XYZ> GenerateTopoPointsFromGraph(List<XYZ> floorBoundary,
            List<System.Windows.Point> graphPoints, string direction, double maxHeight,
            XYZ center, (double Width, double Length) dimensions)
        {
            var topoPoints = new List<XYZ>();

            try
            {
                // Add boundary points at base elevation
                foreach (var point in floorBoundary)
                {
                    topoPoints.Add(point);
                }

                // Generate interior points based on graph profile
                var gridSize = 10; // 10x10 grid
                var stepX = dimensions.Width / gridSize;
                var stepY = dimensions.Length / gridSize;

                var minX = center.X - dimensions.Width / 2;
                var minY = center.Y - dimensions.Length / 2;

                for (int i = 0; i <= gridSize; i++)
                {
                    for (int j = 0; j <= gridSize; j++)
                    {
                        var x = minX + i * stepX;
                        var y = minY + j * stepY;

                        // Calculate elevation based on graph profile and direction
                        var elevation = CalculateElevationFromGraph(x, y, center, dimensions,
                            graphPoints, direction, maxHeight);

                        topoPoints.Add(new XYZ(x, y, center.Z + elevation));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating topo points: {ex.Message}");
            }

            return topoPoints;
        }

        private double CalculateElevationFromGraph(double x, double y, XYZ center,
            (double Width, double Length) dimensions, List<System.Windows.Point> graphPoints,
            string direction, double maxHeight)
        {
            try
            {
                // Calculate position relative to center (0-1 range)
                double relativePosition;

                switch (direction.ToLower())
                {
                    case "x":
                    case "horizontal":
                        relativePosition = (x - (center.X - dimensions.Width / 2)) / dimensions.Width;
                        break;
                    case "y":
                    case "vertical":
                        relativePosition = (y - (center.Y - dimensions.Length / 2)) / dimensions.Length;
                        break;
                    case "radial":
                        var distanceFromCenter = Math.Sqrt(
                            Math.Pow(x - center.X, 2) + Math.Pow(y - center.Y, 2));
                        var maxDistance = Math.Max(dimensions.Width, dimensions.Length) / 2;
                        relativePosition = Math.Min(distanceFromCenter / maxDistance, 1.0);
                        break;
                    default:
                        relativePosition = (x - (center.X - dimensions.Width / 2)) / dimensions.Width;
                        break;
                }

                // Clamp to 0-1 range
                relativePosition = Math.Max(0, Math.Min(1, relativePosition));

                // Interpolate height from graph points
                var graphX = relativePosition; // 0 to 1
                var height = InterpolateHeightFromGraph(graphPoints, graphX);

                return height * maxHeight;
            }
            catch
            {
                return 0;
            }
        }

        private double InterpolateHeightFromGraph(List<System.Windows.Point> graphPoints, double x)
        {
            if (!graphPoints.Any()) return 0;

            // Sort points by X coordinate
            var sortedPoints = graphPoints.OrderBy(p => p.X).ToList();

            // Find the two points to interpolate between
            if (x <= sortedPoints.First().X) return sortedPoints.First().Y;
            if (x >= sortedPoints.Last().X) return sortedPoints.Last().Y;

            for (int i = 0; i < sortedPoints.Count - 1; i++)
            {
                var p1 = sortedPoints[i];
                var p2 = sortedPoints[i + 1];

                if (x >= p1.X && x <= p2.X)
                {
                    // Linear interpolation
                    var t = (x - p1.X) / (p2.X - p1.X);
                    return p1.Y + t * (p2.Y - p1.Y);
                }
            }

            return 0;
        }
    }
}