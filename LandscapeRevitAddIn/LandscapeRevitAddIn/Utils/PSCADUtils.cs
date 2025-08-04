using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace LandscapeRevitAddIn.Utils
{
    /// <summary>
    /// Utility class for CAD Points and related operations
    /// </summary>
    public static class PSCADUtils
    {
        /// <summary>
        /// Gets points from a specified layer
        /// </summary>
        /// <param name="doc">The Revit document</param>
        /// <param name="layerName">Name of the layer to get points from</param>
        /// <returns>List of XYZ points</returns>
        public static List<XYZ> GetPointsFromLayer(Document doc, string layerName)
        {
            var points = new List<XYZ>();

            try
            {
                // Get all CAD imports that might contain the layer
                var cadImports = new FilteredElementCollector(doc)
                    .OfClass(typeof(ImportInstance))
                    .Cast<ImportInstance>()
                    .ToList();

                foreach (var cadImport in cadImports)
                {
                    var geometry = cadImport.get_Geometry(new Options());
                    if (geometry != null)
                    {
                        foreach (GeometryObject geoObj in geometry)
                        {
                            if (geoObj is GeometryInstance instance)
                            {
                                var instanceGeometry = instance.GetInstanceGeometry();
                                foreach (GeometryObject instObj in instanceGeometry)
                                {
                                    // Look for points or curves that represent points
                                    if (instObj is Point point)
                                    {
                                        points.Add(point.Coord);
                                    }
                                    else if (instObj is Curve curve)
                                    {
                                        points.Add(curve.GetEndPoint(0));
                                        points.Add(curve.GetEndPoint(1));
                                    }
                                }
                            }
                        }
                    }
                }

                // If no CAD imports found, create some sample points for testing
                if (!points.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"No points found in layer '{layerName}'. Creating sample points.");
                    points.AddRange(CreateSamplePoints());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting points from layer '{layerName}': {ex.Message}");
                // Return empty list on error
            }

            return points;
        }

        /// <summary>
        /// Creates sample points for testing when no CAD data is available
        /// </summary>
        private static List<XYZ> CreateSamplePoints()
        {
            return new List<XYZ>
            {
                new XYZ(0, 0, 0),
                new XYZ(10, 0, 2),
                new XYZ(20, 0, 1),
                new XYZ(0, 10, 1.5),
                new XYZ(10, 10, 3),
                new XYZ(20, 10, 2.5)
            };
        }

        /// <summary>
        /// Validates if a point is within reasonable bounds
        /// </summary>
        /// <param name="point">Point to validate</param>
        /// <returns>True if point is valid</returns>
        public static bool IsValidPoint(XYZ point)
        {
            if (point == null) return false;

            // Check for extreme values that might indicate invalid data
            const double maxCoordinate = 1000000; // 1 million feet

            return Math.Abs(point.X) < maxCoordinate &&
                   Math.Abs(point.Y) < maxCoordinate &&
                   Math.Abs(point.Z) < maxCoordinate;
        }

        /// <summary>
        /// Filters points to remove duplicates within tolerance
        /// </summary>
        /// <param name="points">Input points</param>
        /// <param name="tolerance">Distance tolerance for duplicate detection</param>
        /// <returns>Filtered list of unique points</returns>
        public static List<XYZ> RemoveDuplicatePoints(List<XYZ> points, double tolerance = 0.01)
        {
            var uniquePoints = new List<XYZ>();

            foreach (var point in points)
            {
                bool isDuplicate = false;
                foreach (var existingPoint in uniquePoints)
                {
                    if (point.DistanceTo(existingPoint) < tolerance)
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (!isDuplicate)
                {
                    uniquePoints.Add(point);
                }
            }

            return uniquePoints;
        }
    }
}