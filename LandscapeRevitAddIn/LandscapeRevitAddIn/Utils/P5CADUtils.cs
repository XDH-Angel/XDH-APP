using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LandscapeRevitAddIn.Utils
{
    /// <summary>
    /// Provides utility methods specific to Panel 05 for handling CAD data,
    /// primarily for extracting points from layers.
    /// </summary>
    public static class P5CADUtils
    {
        /// <summary>
        /// Gets all layers from a CAD file that contain geometric points.
        /// </summary>
        public static Dictionary<string, List<XYZ>> GetLayersWithPoints(ImportInstance cadFile)
        {
            var layersWithPoints = new Dictionary<string, List<XYZ>>();
            if (cadFile == null) return layersWithPoints;

            var doc = cadFile.Document;
            var transform = cadFile.GetTransform();
            var options = new Options { IncludeNonVisibleObjects = true };

            try
            {
                var geoElement = cadFile.get_Geometry(options);
                if (geoElement == null) return layersWithPoints;

                ExtractAllPoints(geoElement, layersWithPoints, transform, doc);

                // Return only layers that actually contain points
                return layersWithPoints.Where(kvp => kvp.Value.Any()).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting layers with points: {ex.Message}");
                return new Dictionary<string, List<XYZ>>();
            }
        }

        /// <summary>
        /// Recursively extracts points from geometry and organizes them by layer.
        /// </summary>
        private static void ExtractAllPoints(GeometryElement geoElement, Dictionary<string, List<XYZ>> layers, Transform transform, Document doc)
        {
            foreach (var geoObj in geoElement)
            {
                if (geoObj is GeometryInstance instance)
                {
                    // Handle nested blocks by recursively calling with the instance's transform
                    ExtractAllPoints(instance.GetInstanceGeometry(), layers, transform.Multiply(instance.Transform), doc);
                    continue;
                }

                string layerName = GetLayerName(geoObj, doc);
                if (string.IsNullOrEmpty(layerName)) continue;

                if (!layers.ContainsKey(layerName))
                {
                    layers[layerName] = new List<XYZ>();
                }

                // Extract points from various geometry types
                if (geoObj is Point point)
                {
                    layers[layerName].Add(transform.OfPoint(point.Coord));
                }
                else if (geoObj is Line line)
                {
                    layers[layerName].Add(transform.OfPoint(line.GetEndPoint(0)));
                }
                else if (geoObj is PolyLine polyline)
                {
                    layers[layerName].AddRange(polyline.GetCoordinates().Select(p => transform.OfPoint(p)));
                }
                else if (geoObj is Arc arc)
                {
                    layers[layerName].Add(transform.OfPoint(arc.GetEndPoint(0)));
                }
            }
        }

        /// <summary>
        /// Extracts a clean layer name from a geometry object's graphics style.
        /// </summary>
        private static string GetLayerName(GeometryObject geoObj, Document doc)
        {
            if (geoObj.GraphicsStyleId == null || geoObj.GraphicsStyleId == ElementId.InvalidElementId)
            {
                return null; // No style, no layer
            }
            try
            {
                var graphicsStyle = doc.GetElement(geoObj.GraphicsStyleId) as GraphicsStyle;
                // The category name is the most reliable source for the layer name
                return graphicsStyle?.GraphicsStyleCategory?.Name;
            }
            catch
            {
                return null;
            }
        }
    }
}
