// Commands/Panel06/BySurfaceAlignCommand.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using LandscapeRevitAddIn.UI.Windows.Panel06;
using LandscapeRevitAddIn.Utils; // Added to use SurfaceAlignmentUtils from Utils folder

namespace LandscapeRevitAddIn.Commands.Panel06
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class BySurfaceAlignCommand : IExternalCommand
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

                // Show the By Surface window
                var window = new BySurfaceAlignWindow(uiDoc);
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
                message = $"Error in By Surface Align command: {ex.Message}";
                return Result.Failed;
            }
        }
    }

    // Selection filter for any surface
    public class AnySurfaceSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Floor || elem is RoofBase || elem is Wall || elem is FamilyInstance;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return reference.ElementReferenceType == ElementReferenceType.REFERENCE_TYPE_SURFACE;
        }
    }

    // Panel06-specific utility class (renamed to avoid conflicts with Utils/SurfaceAlignmentUtils)
    public static class BySurfaceAlignmentUtils
    {
        public static bool AlignFloorEdgesToSurface(Document doc, List<Reference> floorEdges, Reference surfaceRef)
        {
            try
            {
                // Get the surface plane
                Plane surfacePlane = GetPlaneFromSurface(doc, surfaceRef);
                if (surfacePlane == null)
                    return false;

                // Group edges by floor
                var floorGroups = new Dictionary<Floor, List<Reference>>();
                foreach (var edgeRef in floorEdges)
                {
                    Floor floor = EdgeAlignmentUtils.GetFloorFromEdge(doc, edgeRef);
                    if (floor != null)
                    {
                        if (!floorGroups.ContainsKey(floor))
                            floorGroups[floor] = new List<Reference>();
                        floorGroups[floor].Add(edgeRef);
                    }
                }

                using (Transaction trans = new Transaction(doc, "Align Floor Edges to Surface"))
                {
                    trans.Start();

                    foreach (var floorGroup in floorGroups)
                    {
                        Floor floor = floorGroup.Key;
                        List<Reference> edges = floorGroup.Value;

                        if (!AlignFloorEdgesToPlane(doc, floor, edges, surfacePlane))
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to align edges for floor {floor.Id}");
                        }
                    }

                    trans.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error aligning floor edges to surface: {ex.Message}");
                return false;
            }
        }

        private static Plane GetPlaneFromSurface(Document doc, Reference surfaceRef)
        {
            try
            {
                Element element = doc.GetElement(surfaceRef);
                GeometryObject geoObj = element.GetGeometryObjectFromReference(surfaceRef);

                if (geoObj is Face face)
                {
                    // Get a plane from the face
                    if (face is PlanarFace planarFace)
                    {
                        return Plane.CreateByNormalAndOrigin(planarFace.FaceNormal, planarFace.Origin);
                    }
                    else
                    {
                        // For non-planar faces, get a best-fit plane
                        BoundingBoxUV bbox = face.GetBoundingBox();
                        XYZ center = face.Evaluate((bbox.Min + bbox.Max) * 0.5);
                        XYZ normal = face.ComputeNormal((bbox.Min + bbox.Max) * 0.5);
                        return Plane.CreateByNormalAndOrigin(normal, center);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting plane from surface: {ex.Message}");
            }
            return null;
        }

        private static bool AlignFloorEdgesToPlane(Document doc, Floor floor, List<Reference> edges, Plane plane)
        {
            try
            {
                // Get floor boundary curves from geometry
                List<Curve> floorCurves = GetFloorBoundaryCurves(floor);
                if (floorCurves == null || floorCurves.Count == 0)
                    return false;

                // Get floor properties for recreation
                FloorType floorType = doc.GetElement(floor.GetTypeId()) as FloorType;
                Level level = doc.GetElement(floor.LevelId) as Level;

                if (floorType == null || level == null)
                    return false;

                List<Curve> newCurves = new List<Curve>(floorCurves);

                // For each edge to align, find corresponding boundary curve and project it
                foreach (Reference edgeRef in edges)
                {
                    Line edgeLine = EdgeAlignmentUtils.GetEdgeLine(doc, edgeRef);
                    if (edgeLine == null) continue;

                    // Find the closest boundary curve to this edge
                    int closestCurveIndex = FindClosestCurveIndex(newCurves, edgeLine);
                    if (closestCurveIndex >= 0)
                    {
                        // Project curve to plane
                        Curve projectedCurve = ProjectCurveToPlane(newCurves[closestCurveIndex], plane);
                        if (projectedCurve != null)
                        {
                            newCurves[closestCurveIndex] = projectedCurve;
                        }
                    }
                }

                // Delete old floor
                doc.Delete(floor.Id);

                // FIXED: Create new floor using current API
                Floor newFloor = Floor.Create(doc, new List<CurveLoop> { CurveLoop.Create(newCurves) }, floorType.Id, level.Id);

                return newFloor != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error aligning floor edges to plane: {ex.Message}");
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

        private static int FindClosestCurveIndex(List<Curve> curves, Line targetLine)
        {
            double minDistance = double.MaxValue;
            int closestIndex = -1;

            for (int i = 0; i < curves.Count; i++)
            {
                XYZ curveCenter = curves[i].Evaluate(0.5, true);
                XYZ targetCenter = targetLine.Evaluate(0.5, true);
                double distance = curveCenter.DistanceTo(targetCenter);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = i;
                }
            }

            return closestIndex;
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