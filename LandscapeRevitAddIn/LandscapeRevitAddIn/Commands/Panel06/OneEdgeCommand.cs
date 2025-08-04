// Commands/Panel06/OneEdgeCommand.cs
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
    public class OneEdgeCommand : IExternalCommand
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

                // Show the One Edge window
                var window = new OneEdgeWindow(uiDoc);
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
                message = $"Error in One Edge Align command: {ex.Message}";
                return Result.Failed;
            }
        }
    }

    // Selection filter for floor edges
    public class FloorEdgeSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Floor;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            try
            {
                // Check if the reference is an edge
                return reference.ElementReferenceType == ElementReferenceType.REFERENCE_TYPE_LINEAR;
            }
            catch
            {
                return false;
            }
        }
    }

    // Selection filter for reference edges (can be from any element)
    public class ReferenceEdgeSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem != null;
        }

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

    // Utility class for edge alignment operations
    public static class EdgeAlignmentUtils
    {
        public static Line GetEdgeLine(Document doc, Reference edgeRef)
        {
            try
            {
                Element element = doc.GetElement(edgeRef);
                GeometryObject geoObj = element.GetGeometryObjectFromReference(edgeRef);

                if (geoObj is Edge edge)
                {
                    return Line.CreateBound(edge.Evaluate(0), edge.Evaluate(1));
                }
                else if (geoObj is Line line)
                {
                    return line;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting edge line: {ex.Message}");
            }
            return null;
        }

        public static Floor GetFloorFromEdge(Document doc, Reference edgeRef)
        {
            try
            {
                Element element = doc.GetElement(edgeRef);
                return element as Floor;
            }
            catch
            {
                return null;
            }
        }

        public static bool AlignFloorEdgesToReference(Document doc, List<Reference> floorEdges, Reference referenceEdge)
        {
            try
            {
                Line referenceLine = GetEdgeLine(doc, referenceEdge);
                if (referenceLine == null)
                    return false;

                // Group edges by floor
                var floorGroups = floorEdges.GroupBy(edge => GetFloorFromEdge(doc, edge))
                    .Where(group => group.Key != null)
                    .ToList();

                using (Transaction trans = new Transaction(doc, "Align Floor Edges to Reference"))
                {
                    trans.Start();

                    foreach (var floorGroup in floorGroups)
                    {
                        Floor floor = floorGroup.Key;
                        List<Reference> edges = floorGroup.ToList();

                        if (!AlignFloorEdges(doc, floor, edges, referenceLine))
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
                System.Diagnostics.Debug.WriteLine($"Error aligning floor edges: {ex.Message}");
                return false;
            }
        }

        private static bool AlignFloorEdges(Document doc, Floor floor, List<Reference> edges, Line referenceLine)
        {
            try
            {
                // Get floor boundary curves from geometry
                List<Curve> floorCurves = GetFloorBoundaryCurves(floor);
                if (floorCurves == null || floorCurves.Count == 0)
                    return false;

                List<Curve> newCurves = new List<Curve>(floorCurves);

                // For each edge to align, find corresponding boundary curve and modify it
                foreach (Reference edgeRef in edges)
                {
                    Line edgeLine = GetEdgeLine(doc, edgeRef);
                    if (edgeLine == null) continue;

                    // Find the closest boundary curve to this edge
                    int closestCurveIndex = FindClosestCurveIndex(newCurves, edgeLine);
                    if (closestCurveIndex >= 0)
                    {
                        // Create aligned curve
                        Curve alignedCurve = CreateAlignedCurve(newCurves[closestCurveIndex], referenceLine);
                        if (alignedCurve != null)
                        {
                            newCurves[closestCurveIndex] = alignedCurve;
                        }
                    }
                }

                // Update the floor with modified curves
                return UpdateFloorSketch(doc, floor, newCurves);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AlignFloorEdges: {ex.Message}");
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

        private static Curve CreateAlignedCurve(Curve originalCurve, Line referenceLine)
        {
            try
            {
                // Get original curve properties
                XYZ start = originalCurve.GetEndPoint(0);
                XYZ end = originalCurve.GetEndPoint(1);

                // Project points onto reference line
                XYZ refStart = referenceLine.GetEndPoint(0);
                XYZ refEnd = referenceLine.GetEndPoint(1);
                XYZ refDirection = (refEnd - refStart).Normalize();

                // Calculate projection of original points onto reference line direction
                XYZ startProjected = refStart + refDirection * ((start - refStart).DotProduct(refDirection));
                XYZ endProjected = refStart + refDirection * ((end - refStart).DotProduct(refDirection));

                // Create new line with same direction as reference but at original curve location
                XYZ midPoint = (start + end) * 0.5;
                double length = start.DistanceTo(end);

                XYZ newStart = midPoint - refDirection * (length * 0.5);
                XYZ newEnd = midPoint + refDirection * (length * 0.5);

                return Line.CreateBound(newStart, newEnd);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating aligned curve: {ex.Message}");
                return null;
            }
        }

        private static bool UpdateFloorSketch(Document doc, Floor floor, List<Curve> newCurves)
        {
            try
            {
                // Get floor properties
                FloorType floorType = doc.GetElement(floor.GetTypeId()) as FloorType;
                Level level = doc.GetElement(floor.LevelId) as Level;

                if (floorType == null || level == null)
                    return false;

                // Delete old floor
                doc.Delete(floor.Id);

                // FIXED: Create new floor using current API
                Floor newFloor = Floor.Create(doc, new List<CurveLoop> { CurveLoop.Create(newCurves) }, floorType.Id, level.Id);

                return newFloor != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating floor sketch: {ex.Message}");
                return false;
            }
        }
    }
}