// Commands/Panel06/TwoEdgeAlignCommand.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using LandscapeRevitAddIn.UI.Windows.Panel06;

namespace LandscapeRevitAddIn.Commands.Panel06
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class TwoEdgeAlignCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;

                if (doc == null)
                {
                    message = "No active document found.";
                    return Result.Failed;
                }

                // Show the Two Edge window
                var window = new TwoEdgeAlignWindow(uiDoc);
                bool? result = window.ShowDialog();

                if (result == true)
                {
                    return Result.Succeeded;
                }
                else
                {
                    return Result.Cancelled;
                }
            }
            catch (Exception ex)
            {
                message = $"Error in Two Edge Align command: {ex.Message}";
                return Result.Failed;
            }
        }
    }

    // Utility class for two-edge alignment operations
    public static class TwoEdgeAlignmentUtils
    {
        public static bool AlignFloorToTwoEdges(Document doc, Floor targetFloor, Reference edge1, Reference edge2)
        {
            try
            {
                Line line1 = EdgeAlignmentUtils.GetEdgeLine(doc, edge1);
                Line line2 = EdgeAlignmentUtils.GetEdgeLine(doc, edge2);

                if (line1 == null || line2 == null)
                    return false;

                // Create a plane from the two edges
                Plane virtualPlane = CreatePlaneFromTwoLines(line1, line2);
                if (virtualPlane == null)
                    return false;

                return ProjectFloorToPlane(doc, targetFloor, virtualPlane);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error aligning floor to two edges: {ex.Message}");
                return false;
            }
        }

        private static Plane CreatePlaneFromTwoLines(Line line1, Line line2)
        {
            try
            {
                // Get points from both lines
                XYZ p1 = line1.GetEndPoint(0);
                XYZ p2 = line1.GetEndPoint(1);
                XYZ p3 = line2.GetEndPoint(0);
                XYZ p4 = line2.GetEndPoint(1);

                // Try to find the best plane that represents both lines
                // Method 1: If lines are parallel, use any three points
                XYZ dir1 = (p2 - p1).Normalize();
                XYZ dir2 = (p4 - p3).Normalize();

                if (Math.Abs(dir1.DotProduct(dir2)) > 0.99) // Lines are nearly parallel
                {
                    // Use three points to define the plane
                    XYZ v1 = p2 - p1;
                    XYZ v2 = p3 - p1;
                    XYZ normal = v1.CrossProduct(v2).Normalize();
                    return Plane.CreateByNormalAndOrigin(normal, p1);
                }
                else
                {
                    // Lines are not parallel - find the best-fit plane
                    // Use the midpoints and create a plane
                    XYZ mid1 = (p1 + p2) * 0.5;
                    XYZ mid2 = (p3 + p4) * 0.5;
                    XYZ midConnection = mid2 - mid1;

                    // Create normal as average of both line directions and connection vector
                    XYZ normal = dir1.CrossProduct(midConnection).Normalize();
                    return Plane.CreateByNormalAndOrigin(normal, mid1);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating plane from two lines: {ex.Message}");
                return null;
            }
        }

        private static bool ProjectFloorToPlane(Document doc, Floor floor, Plane plane)
        {
            try
            {
                using (Transaction trans = new Transaction(doc, "Align Floor to Two Edges"))
                {
                    trans.Start();

                    // Get floor boundary curves from geometry
                    List<Curve> floorCurves = GetFloorBoundaryCurves(floor);
                    if (floorCurves == null || floorCurves.Count == 0)
                    {
                        trans.RollBack();
                        return false;
                    }

                    // Get floor properties for recreation
                    FloorType floorType = doc.GetElement(floor.GetTypeId()) as FloorType;
                    Level level = doc.GetElement(floor.LevelId) as Level;

                    if (floorType == null || level == null)
                    {
                        trans.RollBack();
                        return false;
                    }

                    // Project all curves to the plane
                    List<Curve> projectedCurves = new List<Curve>();

                    foreach (Curve curve in floorCurves)
                    {
                        Curve projectedCurve = ProjectCurveToPlane(curve, plane);
                        if (projectedCurve != null)
                        {
                            projectedCurves.Add(projectedCurve);
                        }
                    }

                    if (projectedCurves.Count == 0)
                    {
                        trans.RollBack();
                        return false;
                    }

                    // Delete the old floor
                    doc.Delete(floor.Id);

                    // FIXED: Create new floor using current API
                    Floor newFloor = Floor.Create(doc, new List<CurveLoop> { CurveLoop.Create(projectedCurves) }, floorType.Id, level.Id);

                    trans.Commit();
                    return newFloor != null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error projecting floor to plane: {ex.Message}");
                return false;
            }
        }

        private static List<Curve> GetFloorBoundaryCurves(Floor floor)
        {
            try
            {
                List<Curve> curves = new List<Curve>();

                // Get floor geometry
                Options geometryOptions = new Options();
                geometryOptions.ComputeReferences = true;
                GeometryElement geometryElement = floor.get_Geometry(geometryOptions);

                foreach (GeometryObject geometryObject in geometryElement)
                {
                    if (geometryObject is Solid solid)
                    {
                        // Get the bottom face edges (floor boundary)
                        foreach (Face face in solid.Faces)
                        {
                            if (face is PlanarFace planarFace)
                            {
                                // Check if this is the bottom face (facing down)
                                XYZ normal = planarFace.FaceNormal;
                                if (Math.Abs(normal.Z + 1.0) < 0.1) // Facing down
                                {
                                    EdgeArrayArray edgeLoops = planarFace.EdgeLoops;
                                    foreach (EdgeArray edgeLoop in edgeLoops)
                                    {
                                        foreach (Edge edge in edgeLoop)
                                        {
                                            curves.Add(edge.AsCurve());
                                        }
                                    }
                                    return curves; // Return first bottom face found
                                }
                            }
                        }
                    }
                }

                return curves;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting floor boundary curves: {ex.Message}");
                return null;
            }
        }

        private static Curve ProjectCurveToPlane(Curve curve, Plane plane)
        {
            try
            {
                XYZ start = curve.GetEndPoint(0);
                XYZ end = curve.GetEndPoint(1);

                // Project both endpoints to the plane
                XYZ projectedStart = ProjectPointToPlane(start, plane);
                XYZ projectedEnd = ProjectPointToPlane(end, plane);

                // Create new line with projected points
                return Line.CreateBound(projectedStart, projectedEnd);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error projecting curve to plane: {ex.Message}");
                return null;
            }
        }

        private static XYZ ProjectPointToPlane(XYZ point, Plane plane)
        {
            try
            {
                // Project point onto plane
                XYZ vectorToPoint = point - plane.Origin;
                double distanceAlongNormal = vectorToPoint.DotProduct(plane.Normal);
                return point - (distanceAlongNormal * plane.Normal);
            }
            catch
            {
                return point; // Return original point if projection fails
            }
        }
    }
}