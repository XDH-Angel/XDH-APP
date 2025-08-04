// Utils/SurfaceAlignmentUtils.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace LandscapeRevitAddIn.Utils
{
    public static class SurfaceAlignmentUtils
    {
        /// <summary>
        /// Align a floor to match a reference surface
        /// </summary>
        public static bool AlignFloorToSurface(Document doc, Floor floor, Reference surfaceRef, double offset = 0.0)
        {
            try
            {
                using (Transaction trans = new Transaction(doc, "Align Floor to Surface"))
                {
                    trans.Start();

                    // Get the reference surface
                    var element = doc.GetElement(surfaceRef);
                    var referenceFace = element.GetGeometryObjectFromReference(surfaceRef) as Face;

                    if (!(referenceFace is PlanarFace planarFace))
                    {
                        trans.RollBack();
                        return false;
                    }

                    // Get surface plane with offset
                    var surfacePlane = GetPlaneWithOffset(planarFace, offset);
                    if (surfacePlane == null)
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

                    // Get floor boundary curves
                    List<Curve> floorCurves = GetFloorBoundaryCurves(floor);
                    if (floorCurves == null || floorCurves.Count == 0)
                    {
                        trans.RollBack();
                        return false;
                    }

                    // Project floor curves to the surface plane
                    CurveArray projectedCurves = new CurveArray();
                    foreach (Curve curve in floorCurves)
                    {
                        Curve projectedCurve = ProjectCurveToPlane(curve, surfacePlane);
                        if (projectedCurve != null)
                        {
                            projectedCurves.Append(projectedCurve);
                        }
                    }

                    if (projectedCurves.Size == 0)
                    {
                        trans.RollBack();
                        return false;
                    }

                    // Delete the old floor
                    doc.Delete(floor.Id);

                    // FIXED: Create new floor using current API
                    Floor newFloor = Floor.Create(doc, new List<CurveLoop> { CurveLoop.Create(projectedCurves.Cast<Curve>().ToList()) }, floorType.Id, level.Id);

                    trans.Commit();
                    return newFloor != null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error aligning floor to surface: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get plane from face with optional offset
        /// </summary>
        private static Plane GetPlaneWithOffset(PlanarFace face, double offset)
        {
            try
            {
                var normal = face.FaceNormal;
                var origin = face.Origin;

                // Apply offset along the normal direction
                if (Math.Abs(offset) > 0.001) // Only apply if offset is significant
                {
                    origin = origin + (offset * normal);
                }

                return Plane.CreateByNormalAndOrigin(normal, origin);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get floor boundary curves from geometry
        /// </summary>
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

        /// <summary>
        /// Project a curve to a plane
        /// </summary>
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

        /// <summary>
        /// Project a point to a plane
        /// </summary>
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
    }
}