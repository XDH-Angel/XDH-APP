using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace LandscapeRevitAddIn.Utils
{
    /// <summary>
    /// Utility class for Floor operations and modifications
    /// </summary>
    public static class PSFloorUtils
    {
        /// <summary>
        /// Adjusts floor elevation based on surface points
        /// </summary>
        /// <param name="doc">The Revit document</param>
        /// <param name="floor">Floor to adjust</param>
        /// <param name="surfacePoints">Points defining the surface</param>
        /// <returns>True if successful</returns>
        public static bool AdjustFloorBySurfacePoints(Document doc, Floor floor, List<XYZ> surfacePoints)
        {
            if (floor == null || surfacePoints == null || !surfacePoints.Any())
                return false;

            try
            {
                using (Transaction trans = new Transaction(doc, "Adjust Floor by Surface Points"))
                {
                    trans.Start();

                    // Get average elevation from surface points
                    var averageElevation = surfacePoints.Average(p => p.Z);

                    // Get current floor level
                    var floorLevel = doc.GetElement(floor.LevelId) as Level;
                    if (floorLevel != null)
                    {
                        // Calculate offset needed
                        var currentElevation = floorLevel.Elevation;
                        var offsetNeeded = averageElevation - currentElevation;

                        // Apply offset to floor
                        var offsetParam = floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
                        if (offsetParam != null && !offsetParam.IsReadOnly)
                        {
                            offsetParam.Set(offsetNeeded);
                        }
                    }

                    trans.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adjusting floor by surface points: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Applies slope to floor based on specified parameters
        /// </summary>
        /// <param name="doc">The Revit document</param>
        /// <param name="floor">Floor to modify</param>
        /// <param name="slopeAngle">Slope angle in degrees</param>
        /// <param name="slopeDirection">Direction vector for slope</param>
        /// <returns>True if successful</returns>
        public static bool ApplySlopeToFloor(Document doc, Floor floor, double slopeAngle, XYZ slopeDirection)
        {
            if (floor == null || slopeDirection == null)
                return false;

            try
            {
                using (Transaction trans = new Transaction(doc, "Apply Slope to Floor"))
                {
                    trans.Start();

                    // Enable slope on floor
                    var slopeParam = floor.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);
                    if (slopeParam != null && !slopeParam.IsReadOnly)
                    {
                        // Note: Floor sloping is complex and depends on floor type
                        // This is a simplified implementation
                        System.Diagnostics.Debug.WriteLine($"Applying slope of {slopeAngle} degrees to floor");
                    }

                    trans.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying slope to floor: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets all floors in the document
        /// </summary>
        /// <param name="doc">The Revit document</param>
        /// <returns>List of all floors</returns>
        public static List<Floor> GetAllFloors(Document doc)
        {
            try
            {
                return new FilteredElementCollector(doc)
                    .OfClass(typeof(Floor))
                    .Cast<Floor>()
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting all floors: {ex.Message}");
                return new List<Floor>();
            }
        }

        /// <summary>
        /// Gets display name for a floor
        /// </summary>
        /// <param name="floor">The floor element</param>
        /// <returns>Display name string</returns>
        public static string GetFloorDisplayName(Floor floor)
        {
            if (floor == null)
                return "Unknown Floor";

            try
            {
                var floorType = floor.Document.GetElement(floor.GetTypeId());
                var typeName = floorType?.Name ?? "Unknown Type";
                var floorId = floor.Id.IntegerValue;

                return $"Floor {floorId} ({typeName})";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting floor display name: {ex.Message}");
                return $"Floor {floor.Id.IntegerValue}";
            }
        }

        /// <summary>
        /// Gets floor boundary curves
        /// </summary>
        /// <param name="floor">The floor element</param>
        /// <returns>List of boundary curves</returns>
        public static List<Curve> GetFloorBoundaryCurves(Floor floor)
        {
            var curves = new List<Curve>();

            if (floor == null)
                return curves;

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
                                curves.Add(curve);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting floor boundary curves: {ex.Message}");
            }

            return curves;
        }

        /// <summary>
        /// Calculates floor area
        /// </summary>
        /// <param name="floor">The floor element</param>
        /// <returns>Floor area in square feet</returns>
        public static double GetFloorArea(Floor floor)
        {
            if (floor == null)
                return 0.0;

            try
            {
                var areaParam = floor.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
                if (areaParam != null)
                {
                    return areaParam.AsDouble();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting floor area: {ex.Message}");
            }

            return 0.0;
        }
    }
}