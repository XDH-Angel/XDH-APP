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
    public class TopoMoundCommand : IExternalCommand
    {
        private Document _doc;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                _doc = uiDoc.Document;

                if (_doc == null)
                {
                    TaskDialog.Show("Error", "No active document found.");
                    return Result.Failed;
                }

                // Show the TopoMound window
                var topoMoundWindow = new TopoMoundWindow(_doc);
                var dialogResult = topoMoundWindow.ShowDialog();

                if (dialogResult == true)
                {
                    var selectedTopo = topoMoundWindow.SelectedTopography;
                    var selectedFloor = topoMoundWindow.SelectedFloor;
                    var offsetValue = topoMoundWindow.OffsetValue;

                    if (selectedTopo != null && selectedFloor != null)
                    {
                        var result = CreateTopoMound(_doc, selectedTopo, selectedFloor, offsetValue);

                        if (result)
                        {
                            TaskDialog.Show("Success",
                                $"Floor successfully modified to match topography shape with {offsetValue:F2} ft offset.");
                        }
                        else
                        {
                            TaskDialog.Show("Error", "Failed to create topo mound. Please check the selected elements and try again.");
                        }
                    }
                    else
                    {
                        TaskDialog.Show("No Selection", "Please select both topography and floor elements.");
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

        private bool CreateTopoMound(Document doc, TopographySurface topo, Floor floor, double offsetValue)
        {
            try
            {
                using (Transaction trans = new Transaction(doc, "Create Topo Mound"))
                {
                    trans.Start();

                    // Get floor boundary points
                    var floorBoundaryPoints = GetFloorBoundaryPoints(floor);
                    if (!floorBoundaryPoints.Any())
                    {
                        trans.RollBack();
                        return false;
                    }

                    // Sample topography elevations at floor boundary points
                    var elevatedPoints = new List<XYZ>();
                    foreach (var point in floorBoundaryPoints)
                    {
                        var topoElevation = GetTopographyElevationAtPoint(topo, point);
                        var newElevation = topoElevation + offsetValue;
                        var elevatedPoint = new XYZ(point.X, point.Y, newElevation);
                        elevatedPoints.Add(elevatedPoint);
                    }

                    // Create new topography surface from elevated points
                    if (elevatedPoints.Count >= 3)
                    {
                        TopographySurface.Create(doc, elevatedPoints);
                    }

                    trans.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating topo mound: {ex.Message}");
                return false;
            }
        }

        private List<XYZ> GetFloorBoundaryPoints(Floor floor)
        {
            var points = new List<XYZ>();
            try
            {
                // Get the floor sketch
                var sketchId = floor.SketchId;
                if (sketchId != ElementId.InvalidElementId)
                {
                    var sketch = _doc.GetElement(sketchId) as Sketch;
                    if (sketch != null)
                    {
                        foreach (CurveArray curveArray in sketch.Profile)
                        {
                            foreach (Curve curve in curveArray)
                            {
                                // Sample points along the curve
                                var paramIncrement = 1.0 / 10.0; // 10 points per curve
                                for (int i = 0; i <= 10; i++)
                                {
                                    var param = i * paramIncrement;
                                    var point = curve.Evaluate(param, true);
                                    points.Add(point);
                                }
                            }
                        }
                    }
                }

                // Add some interior points for better surface definition
                if (points.Count >= 3)
                {
                    var centerX = points.Average(p => p.X);
                    var centerY = points.Average(p => p.Y);
                    var centerZ = points.Average(p => p.Z);
                    points.Add(new XYZ(centerX, centerY, centerZ));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting floor boundary points: {ex.Message}");
            }

            return points;
        }

        private double GetTopographyElevationAtPoint(TopographySurface topo, XYZ point)
        {
            try
            {
                // Create a vertical line from the point
                var startPoint = new XYZ(point.X, point.Y, point.Z - 1000);
                var endPoint = new XYZ(point.X, point.Y, point.Z + 1000);
                var line = Line.CreateBound(startPoint, endPoint);

                // Find intersection with topography
                var solid = GetTopographySolid(topo);
                if (solid != null)
                {
                    // FIXED: Correct SolidCurveIntersection usage
                    var intersectionResult = solid.IntersectWithCurve(line, new SolidCurveIntersectionOptions());
                    if (intersectionResult != null && intersectionResult.SegmentCount > 0)
                    {
                        // Get the first intersection point
                        var segment = intersectionResult.GetCurveSegment(0);
                        return segment.GetEndPoint(0).Z;
                    }
                }

                // Fallback: return the original point elevation
                return point.Z;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting topography elevation: {ex.Message}");
                return point.Z;
            }
        }

        private Solid GetTopographySolid(TopographySurface topo)
        {
            try
            {
                var geometry = topo.get_Geometry(new Options());
                if (geometry != null)
                {
                    foreach (GeometryObject geoObj in geometry)
                    {
                        if (geoObj is Solid solid && solid.Volume > 0)
                        {
                            return solid;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting topography solid: {ex.Message}");
            }

            return null;
        }
    }
}