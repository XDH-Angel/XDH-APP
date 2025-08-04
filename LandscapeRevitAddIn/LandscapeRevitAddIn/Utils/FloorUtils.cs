using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace LandscapeRevitAddIn.Utils
{
    public static class FloorUtils
    {
        public static List<FloorType> GetFloorTypes(Document doc)
        {
            try
            {
                return new FilteredElementCollector(doc)
                    .OfClass(typeof(FloorType))
                    .Cast<FloorType>()
                    .OrderBy(ft => ft.Name)
                    .ToList();
            }
            catch
            {
                return new List<FloorType>();
            }
        }

        public static Dictionary<string, List<List<Curve>>> GetLayersWithClosedBoundaries(ImportInstance cadInstance)
        {
            var layersWithBoundaries = new Dictionary<string, List<List<Curve>>>();

            try
            {
                var geoElement = cadInstance.get_Geometry(new Options());

                foreach (GeometryObject geoObj in geoElement)
                {
                    if (geoObj is GeometryInstance geoInstance)
                    {
                        var instanceGeometry = geoInstance.GetInstanceGeometry();
                        ProcessGeometryForBoundaries(instanceGeometry, layersWithBoundaries, cadInstance.Document);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error reading CAD boundaries: " + ex.Message);
            }

            return layersWithBoundaries;
        }

        private static void ProcessGeometryForBoundaries(GeometryElement geometry, Dictionary<string, List<List<Curve>>> layersWithBoundaries, Document doc)
        {
            var curvesByLayer = new Dictionary<string, List<Curve>>();

            foreach (GeometryObject geoObj in geometry)
            {
                string layerName = GetLayerName(geoObj, doc);

                if (geoObj is Line line)
                {
                    AddCurveToLayer(curvesByLayer, layerName, line);
                }
                else if (geoObj is Arc arc)
                {
                    AddCurveToLayer(curvesByLayer, layerName, arc);
                }
                else if (geoObj is PolyLine polyLine)
                {
                    var coordinates = polyLine.GetCoordinates();
                    for (int i = 0; i < coordinates.Count - 1; i++)
                    {
                        var segment = Line.CreateBound(coordinates[i], coordinates[i + 1]);
                        AddCurveToLayer(curvesByLayer, layerName, segment);
                    }
                }
                else if (geoObj is GeometryInstance nestedInstance)
                {
                    var nestedGeometry = nestedInstance.GetInstanceGeometry();
                    ProcessGeometryForBoundaries(nestedGeometry, layersWithBoundaries, doc);
                }
            }

            foreach (var layer in curvesByLayer)
            {
                var closedBoundaries = FindClosedBoundaries(layer.Value);
                if (closedBoundaries.Count > 0)
                {
                    layersWithBoundaries[layer.Key] = closedBoundaries;
                }
            }
        }

        private static void AddCurveToLayer(Dictionary<string, List<Curve>> curvesByLayer, string layerName, Curve curve)
        {
            if (!curvesByLayer.ContainsKey(layerName))
            {
                curvesByLayer[layerName] = new List<Curve>();
            }
            curvesByLayer[layerName].Add(curve);
        }

        private static List<List<Curve>> FindClosedBoundaries(List<Curve> curves)
        {
            var closedBoundaries = new List<List<Curve>>();
            var unusedCurves = new List<Curve>(curves);
            var tolerance = 0.01;

            while (unusedCurves.Count > 0)
            {
                var boundary = new List<Curve>();
                var currentCurve = unusedCurves[0];
                unusedCurves.RemoveAt(0);
                boundary.Add(currentCurve);

                var currentEndPoint = currentCurve.GetEndPoint(1);
                var startPoint = currentCurve.GetEndPoint(0);

                bool foundConnection = true;
                while (foundConnection && unusedCurves.Count > 0)
                {
                    foundConnection = false;

                    for (int i = 0; i < unusedCurves.Count; i++)
                    {
                        var testCurve = unusedCurves[i];
                        var testStart = testCurve.GetEndPoint(0);
                        var testEnd = testCurve.GetEndPoint(1);

                        if (currentEndPoint.DistanceTo(testStart) < tolerance)
                        {
                            boundary.Add(testCurve);
                            currentEndPoint = testEnd;
                            unusedCurves.RemoveAt(i);
                            foundConnection = true;
                            break;
                        }
                        else if (currentEndPoint.DistanceTo(testEnd) < tolerance)
                        {
                            var reversedCurve = testCurve.CreateReversed();
                            boundary.Add(reversedCurve);
                            currentEndPoint = reversedCurve.GetEndPoint(1);
                            unusedCurves.RemoveAt(i);
                            foundConnection = true;
                            break;
                        }
                    }
                }

                // Check if the boundary is closed (minimum 3 curves for a valid polygon)
                if (boundary.Count >= 3 && currentEndPoint.DistanceTo(startPoint) < tolerance)
                {
                    closedBoundaries.Add(boundary);
                }
            }

            return closedBoundaries;
        }

        /// <summary>
        /// Get layer name from geometry object - updated to use same logic as CADUtils
        /// </summary>
        private static string GetLayerName(GeometryObject geoObj, Document doc)
        {
            try
            {
                // Try to get the layer information from the graphics style
                var graphicsStyleId = geoObj.GraphicsStyleId;
                if (graphicsStyleId != null && graphicsStyleId != ElementId.InvalidElementId)
                {
                    var graphicsStyle = doc.GetElement(graphicsStyleId) as GraphicsStyle;
                    if (graphicsStyle != null)
                    {
                        // Get the actual layer name from the graphics style
                        string styleName = graphicsStyle.Name;

                        // Clean up the style name to make it more user-friendly
                        string layerName = CleanLayerName(styleName);

                        return layerName;
                    }
                }

                return "Default Layer";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting layer name: {ex.Message}");
                return "Unknown Layer";
            }
        }

        /// <summary>
        /// Clean up layer name to make it more user-friendly
        /// </summary>
        private static string CleanLayerName(string rawLayerName)
        {
            if (string.IsNullOrWhiteSpace(rawLayerName))
                return "Unnamed Layer";

            string cleaned = rawLayerName.Trim();

            // Remove common prefixes that might be added by Revit
            string[] prefixesToRemove = { "Import Symbol - ", "CAD - ", "DWG - " };
            foreach (string prefix in prefixesToRemove)
            {
                if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = cleaned.Substring(prefix.Length).Trim();
                    break;
                }
            }

            // Handle common CAD layer naming conventions for floors/areas
            var friendlyMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "0", "Default Layer" },
                { "DEFPOINTS", "Reference Points" },
                { "A-FLOR", "Floor Areas" },
                { "A-AREA", "Room Areas" },
                { "L-AREA", "Landscape Areas" },
                { "L-LAWN", "Lawn Areas" },
                { "L-PAVE", "Paved Areas" },
                { "L-DECK", "Deck Areas" },
                { "C-PROP", "Property Lines" },
                { "C-BLDG", "Building Outline" }
            };

            if (friendlyMappings.ContainsKey(cleaned))
            {
                return friendlyMappings[cleaned];
            }

            // Try to parse common naming patterns
            if (cleaned.Contains("-"))
            {
                var parts = cleaned.Split('-');
                if (parts.Length >= 2)
                {
                    string discipline = parts[0].ToUpper();
                    string type = parts[1];

                    // Common discipline codes
                    var disciplineNames = new Dictionary<string, string>
                    {
                        { "A", "Architecture" },
                        { "L", "Landscape" },
                        { "C", "Civil" },
                        { "S", "Structural" },
                        { "M", "Mechanical" },
                        { "E", "Electrical" },
                        { "P", "Plumbing" }
                    };

                    string disciplineName = disciplineNames.ContainsKey(discipline)
                        ? disciplineNames[discipline]
                        : discipline;

                    return $"{disciplineName} - {type}";
                }
            }

            // If all else fails, just clean up the original name
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleaned.ToLower().Replace("_", " "));
        }

        /// <summary>
        /// Create a floor from boundary curves - Updated for Revit 2024 API compatibility
        /// </summary>
        public static Floor CreateFloorFromBoundary(Document doc, List<Curve> boundary, FloorType floorType, Level level)
        {
            try
            {
                // Convert the boundary curves to a CurveLoop
                var curveLoop = CurveLoop.Create(boundary);

                // Create a list of CurveLoops (required by the API)
                var curveLoops = new List<CurveLoop> { curveLoop };

                // Use the full Floor.Create method signature with all required parameters
                // For a flat floor (no slope), we pass null for slopeArrow and 0.0 for slope
                var floor = Floor.Create(doc, curveLoops, floorType.Id, level.Id, false, null, 0.0);
                return floor;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating floor with Floor.Create: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Alternative method to create floor using different API approach
        /// </summary>
        private static Floor CreateFloorWithAlternativeMethod(Document doc, List<Curve> boundary, FloorType floorType, Level level)
        {
            try
            {
                // Create a CurveLoop from the boundary
                var curveLoop = CurveLoop.Create(boundary);
                var curveLoops = new List<CurveLoop> { curveLoop };

                // Use the full parameter set - this method is kept for consistency but may not be needed
                return Floor.Create(doc, curveLoops, floorType.Id, level.Id, false, null, 0.0);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CreateFloorWithAlternativeMethod: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Create floor using Transaction for better error handling
        /// </summary>
        public static Floor CreateFloorWithTransaction(Document doc, List<Curve> boundary, FloorType floorType, Level level)
        {
            using (var transaction = new Transaction(doc, "Create Floor"))
            {
                try
                {
                    transaction.Start();

                    var floor = CreateFloorFromBoundary(doc, boundary, floorType, level);

                    if (floor != null)
                    {
                        transaction.Commit();
                        return floor;
                    }
                    else
                    {
                        transaction.RollBack();
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    System.Diagnostics.Debug.WriteLine($"Error in CreateFloorWithTransaction: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Create a sloped floor from boundary curves
        /// </summary>
        public static Floor CreateSlopedFloor(Document doc, List<Curve> boundary, FloorType floorType, Level level, Line slopeDirection, double slopeAngle)
        {
            try
            {
                var curveLoop = CurveLoop.Create(boundary);
                var curveLoops = new List<CurveLoop> { curveLoop };

                // Create floor with slope parameters
                var floor = Floor.Create(doc, curveLoops, floorType.Id, level.Id, false, slopeDirection, slopeAngle);
                return floor;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating sloped floor: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validate that boundary curves form a valid closed loop
        /// </summary>
        public static bool ValidateBoundary(List<Curve> boundary)
        {
            if (boundary == null || boundary.Count < 3)
                return false;

            try
            {
                var tolerance = 0.01;

                // Check if each curve connects to the next
                for (int i = 0; i < boundary.Count; i++)
                {
                    var currentCurve = boundary[i];
                    var nextCurve = boundary[(i + 1) % boundary.Count];

                    var currentEnd = currentCurve.GetEndPoint(1);
                    var nextStart = nextCurve.GetEndPoint(0);

                    if (currentEnd.DistanceTo(nextStart) > tolerance)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string GetFloorTypeDisplayName(FloorType floorType)
        {
            try
            {
                return floorType.Name;
            }
            catch
            {
                return "Unknown Floor Type";
            }
        }

        public static Level GetDefaultLevel(Document doc)
        {
            try
            {
                return new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .OrderBy(l => l.Elevation)
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get all levels in the document
        /// </summary>
        public static List<Level> GetLevels(Document doc)
        {
            try
            {
                return new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .OrderBy(l => l.Elevation)
                    .ToList();
            }
            catch
            {
                return new List<Level>();
            }
        }
    }
}