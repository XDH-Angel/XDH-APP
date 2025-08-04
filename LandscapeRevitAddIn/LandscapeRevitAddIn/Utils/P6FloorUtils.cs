// Utils/P6FloorUtils.cs - Panel06 specific floor utilities
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace LandscapeRevitAddIn.Utils
{
    public static class P6FloorUtils
    {
        /// <summary>
        /// Align floor edges to reference edge
        /// </summary>
        public static bool AlignFloorEdgesToReferenceEdge(Document doc, List<Edge> edgesToAlign, Edge referenceEdge)
        {
            try
            {
                // Get reference line from edge
                var referenceLine = GetLineFromEdge(referenceEdge);
                if (referenceLine == null) return false;

                foreach (var edge in edgesToAlign)
                {
                    var floor = GetFloorFromEdge(doc, edge);
                    if (floor == null) continue;

                    var sketch = GetFloorSketch(floor);
                    if (sketch == null) continue;

                    // Project edge onto reference line and update floor sketch
                    if (!AlignEdgeToLine(doc, floor, sketch, edge, referenceLine))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error aligning floor edges to reference: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Align floor to virtual surface created from two reference edges
        /// </summary>
        public static bool AlignFloorToVirtualSurfaceFromEdges(Document doc, Floor floor, List<Edge> referenceEdges)
        {
            try
            {
                if (referenceEdges.Count != 2) return false;

                var sketch = GetFloorSketch(floor);
                if (sketch == null) return false;

                // Create virtual plane from two edges
                var line1 = GetLineFromEdge(referenceEdges[0]);
                var line2 = GetLineFromEdge(referenceEdges[1]);

                if (line1 == null || line2 == null) return false;

                var virtualPlane = CreatePlaneFromTwoLines(line1, line2);
                if (virtualPlane == null) return false;

                return ModifyFloorSketchToPlane(doc, floor, sketch, virtualPlane);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error aligning floor to virtual surface: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Align floor edges to reference surface
        /// </summary>
        public static bool AlignFloorEdgesToSurface(Document doc, List<Edge> edgesToAlign, Face referenceSurface)
        {
            try
            {
                var plane = GetPlaneFromFace(referenceSurface);
                if (plane == null) return false;

                foreach (var edge in edgesToAlign)
                {
                    var floor = GetFloorFromEdge(doc, edge);
                    if (floor == null) continue;

                    var sketch = GetFloorSketch(floor);
                    if (sketch == null) continue;

                    if (!AlignEdgeToPlane(doc, floor, sketch, edge, plane))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error aligning floor edges to surface: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get all floors in the document
        /// </summary>
        public static List<Floor> GetAllFloors(Document doc)
        {
            try
            {
                return new FilteredElementCollector(doc)
                    .OfClass(typeof(Floor))
                    .Cast<Floor>()
                    .ToList();
            }
            catch
            {
                return new List<Floor>();
            }
        }

        /// <summary>
        /// Get display name for floor
        /// </summary>
        public static string GetFloorDisplayName(Floor floor)
        {
            try
            {
                var floorType = floor.FloorType;
                return $"{floorType.Name} (ID: {floor.Id})";
            }
            catch
            {
                return $"Floor (ID: {floor.Id})";
            }
        }

        // Helper Methods

        private static Sketch GetFloorSketch(Floor floor)
        {
            try
            {
                var sketchId = floor.SketchId;
                if (sketchId == ElementId.InvalidElementId) return null;

                return floor.Document.GetElement(sketchId) as Sketch;
            }
            catch
            {
                return null;
            }
        }

        private static Line GetLineFromEdge(Edge edge)
        {
            try
            {
                return edge.AsCurve() as Line;
            }
            catch
            {
                return null;
            }
        }

        private static Plane GetPlaneFromFace(Face face)
        {
            try
            {
                if (face is PlanarFace planarFace)
                {
                    return Plane.CreateByNormalAndOrigin(planarFace.FaceNormal, planarFace.Origin);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private static Plane CreatePlaneFromTwoLines(Line line1, Line line2)
        {
            try
            {
                var dir1 = line1.Direction;
                var dir2 = line2.Direction;
                var point1 = line1.GetEndPoint(0);
                var point2 = line2.GetEndPoint(0);

                // Create plane from the two line directions
                var normal = dir1.CrossProduct(dir2);
                if (normal.IsZeroLength())
                {
                    // Lines are parallel, use vector between lines
                    var connectionVector = point2 - point1;
                    normal = dir1.CrossProduct(connectionVector);
                }

                if (normal.IsZeroLength()) return null;

                normal = normal.Normalize();
                return Plane.CreateByNormalAndOrigin(normal, point1);
            }
            catch
            {
                return null;
            }
        }

        private static Floor GetFloorFromEdge(Document doc, Edge edge)
        {
            try
            {
                // This is a simplified approach - in practice, you'd need to 
                // trace back from the edge to find the parent floor element
                // This would require more complex geometry analysis

                // Alternative approach: iterate through all floors and check their geometry
                var floors = new FilteredElementCollector(doc)
                    .OfClass(typeof(Floor))
                    .Cast<Floor>()
                    .ToList();

                foreach (var floor in floors)
                {
                    var floorGeometry = floor.get_Geometry(new Options());
                    if (floorGeometry != null && ContainsEdge(floorGeometry, edge))
                    {
                        return floor;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static bool ContainsEdge(GeometryElement geometry, Edge targetEdge)
        {
            try
            {
                foreach (var geometryObject in geometry)
                {
                    if (geometryObject is Solid solid)
                    {
                        foreach (Edge edge in solid.Edges)
                        {
                            if (AreEdgesEquivalent(edge, targetEdge))
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool AreEdgesEquivalent(Edge edge1, Edge edge2)
        {
            try
            {
                var curve1 = edge1.AsCurve();
                var curve2 = edge2.AsCurve();

                if (curve1 == null || curve2 == null) return false;

                var start1 = curve1.GetEndPoint(0);
                var end1 = curve1.GetEndPoint(1);
                var start2 = curve2.GetEndPoint(0);
                var end2 = curve2.GetEndPoint(1);

                // Check if endpoints are approximately equal (either direction)
                bool forwardMatch = start1.IsAlmostEqualTo(start2) && end1.IsAlmostEqualTo(end2);
                bool reverseMatch = start1.IsAlmostEqualTo(end2) && end1.IsAlmostEqualTo(start2);

                return forwardMatch || reverseMatch;
            }
            catch
            {
                return false;
            }
        }

        private static bool ModifyFloorSketchToPlane(Document doc, Floor floor, Sketch sketch, Plane plane)
        {
            try
            {
                // Get sketch curves and project them onto the plane
                var sketchLines = new FilteredElementCollector(doc)
                    .OfClass(typeof(ModelCurve))
                    .Where(x => x.OwnerViewId == sketch.OwnerViewId)
                    .Cast<ModelCurve>()
                    .ToList();

                foreach (var modelCurve in sketchLines)
                {
                    var curve = modelCurve.GeometryCurve;
                    var projectedCurve = ProjectCurveToPlane(curve, plane);

                    if (projectedCurve != null)
                    {
                        modelCurve.SetGeometryCurve(projectedCurve, true);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error modifying floor sketch: {ex.Message}");
                return false;
            }
        }

        private static bool AlignEdgeToLine(Document doc, Floor floor, Sketch sketch, Edge edge, Line referenceLine)
        {
            try
            {
                // Implementation for aligning specific edge to reference line
                // This would involve modifying the floor sketch geometry to align the edge

                // Get the edge curve
                var edgeCurve = edge.AsCurve();
                if (edgeCurve == null) return false;

                // Project edge endpoints onto reference line
                var edgeStart = edgeCurve.GetEndPoint(0);
                var edgeEnd = edgeCurve.GetEndPoint(1);

                var projectedStart = ProjectPointToLine(edgeStart, referenceLine);
                var projectedEnd = ProjectPointToLine(edgeEnd, referenceLine);

                // Create new aligned curve
                var alignedCurve = Line.CreateBound(projectedStart, projectedEnd);

                // Find corresponding sketch curve and update it
                return UpdateSketchCurve(doc, sketch, edgeCurve, alignedCurve);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error aligning edge to line: {ex.Message}");
                return false;
            }
        }

        private static bool AlignEdgeToPlane(Document doc, Floor floor, Sketch sketch, Edge edge, Plane plane)
        {
            try
            {
                // Implementation for aligning edge to plane
                var edgeCurve = edge.AsCurve();
                if (edgeCurve == null) return false;

                var projectedCurve = ProjectCurveToPlane(edgeCurve, plane);
                if (projectedCurve == null) return false;

                return UpdateSketchCurve(doc, sketch, edgeCurve, projectedCurve);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error aligning edge to plane: {ex.Message}");
                return false;
            }
        }

        private static bool UpdateSketchCurve(Document doc, Sketch sketch, Curve originalCurve, Curve newCurve)
        {
            try
            {
                var sketchLines = new FilteredElementCollector(doc)
                    .OfClass(typeof(ModelCurve))
                    .Where(x => x.OwnerViewId == sketch.OwnerViewId)
                    .Cast<ModelCurve>()
                    .ToList();

                foreach (var modelCurve in sketchLines)
                {
                    var curve = modelCurve.GeometryCurve;
                    if (AreCurvesEquivalent(curve, originalCurve))
                    {
                        modelCurve.SetGeometryCurve(newCurve, true);
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

        private static bool AreCurvesEquivalent(Curve curve1, Curve curve2)
        {
            try
            {
                var start1 = curve1.GetEndPoint(0);
                var end1 = curve1.GetEndPoint(1);
                var start2 = curve2.GetEndPoint(0);
                var end2 = curve2.GetEndPoint(1);

                bool forwardMatch = start1.IsAlmostEqualTo(start2) && end1.IsAlmostEqualTo(end2);
                bool reverseMatch = start1.IsAlmostEqualTo(end2) && end1.IsAlmostEqualTo(start2);

                return forwardMatch || reverseMatch;
            }
            catch
            {
                return false;
            }
        }

        private static Curve ProjectCurveToPlane(Curve curve, Plane plane)
        {
            try
            {
                if (curve is Line line)
                {
                    var start = ProjectPointToPlane(line.GetEndPoint(0), plane);
                    var end = ProjectPointToPlane(line.GetEndPoint(1), plane);
                    return Line.CreateBound(start, end);
                }
                // Handle other curve types as needed
                return null;
            }
            catch
            {
                return null;
            }
        }

        private static XYZ ProjectPointToPlane(XYZ point, Plane plane)
        {
            try
            {
                var vectorToPoint = point - plane.Origin;
                var distanceToPlane = vectorToPoint.DotProduct(plane.Normal);
                return point - distanceToPlane * plane.Normal;
            }
            catch
            {
                return point;
            }
        }

        private static XYZ ProjectPointToLine(XYZ point, Line line)
        {
            try
            {
                var lineStart = line.GetEndPoint(0);
                var lineDirection = line.Direction;
                var vectorToPoint = point - lineStart;
                var projectionLength = vectorToPoint.DotProduct(lineDirection);
                return lineStart + projectionLength * lineDirection;
            }
            catch
            {
                return point;
            }
        }
    } // <-- MISSING CLOSING BRACE WAS HERE!
}