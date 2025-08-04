using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace LandscapeRevitAddIn.Utils
{
    public static class CADUtils
    {
        /// <summary>
        /// Get all linked CAD files in the document
        /// </summary>
        public static List<ImportInstance> GetLinkedCADFiles(Document doc)
        {
            try
            {
                var cadLinks = new FilteredElementCollector(doc)
                    .OfClass(typeof(ImportInstance))
                    .Cast<ImportInstance>()
                    .Where(x => x.IsLinked)
                    .ToList();

                return cadLinks;
            }
            catch (Exception)
            {
                return new List<ImportInstance>();
            }
        }

        /// <summary>
        /// Get all lines from a CAD import
        /// </summary>
        public static List<Line> GetLinesFromCAD(ImportInstance cadInstance)
        {
            var lines = new List<Line>();

            try
            {
                var geoElement = cadInstance.get_Geometry(new Options());

                foreach (GeometryObject geoObj in geoElement)
                {
                    if (geoObj is GeometryInstance geoInstance)
                    {
                        var instanceGeometry = geoInstance.GetInstanceGeometry();

                        foreach (GeometryObject instObj in instanceGeometry)
                        {
                            if (instObj is Line line)
                            {
                                lines.Add(line);
                            }
                            else if (instObj is PolyLine polyLine)
                            {
                                // Convert polyline to individual line segments
                                var coordinates = polyLine.GetCoordinates();
                                for (int i = 0; i < coordinates.Count - 1; i++)
                                {
                                    var segment = Line.CreateBound(coordinates[i], coordinates[i + 1]);
                                    lines.Add(segment);
                                }
                            }
                        }
                    }
                    else if (geoObj is Line directLine)
                    {
                        lines.Add(directLine);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle errors gracefully
                System.Diagnostics.Debug.WriteLine("Error reading CAD geometry: " + ex.Message);
            }

            return lines;
        }

        /// <summary>
        /// Calculate the midpoint of a line
        /// </summary>
        public static XYZ GetLineMidpoint(Line line)
        {
            var start = line.GetEndPoint(0);
            var end = line.GetEndPoint(1);
            return (start + end) / 2.0;
        }

        /// <summary>
        /// Get a user-friendly name for a CAD file
        /// </summary>
        public static string GetCADFileName(ImportInstance cadInstance)
        {
            try
            {
                // Try to get the original file name
                var cadType = cadInstance.Document.GetElement(cadInstance.GetTypeId()) as CADLinkType;
                if (cadType != null)
                {
                    return System.IO.Path.GetFileNameWithoutExtension(cadType.Name);
                }

                return "CAD File " + cadInstance.Id.Value;
            }
            catch
            {
                return "Unknown CAD File";
            }
        }

        /// <summary>
        /// Get layer information from CAD file
        /// </summary>
        public static Dictionary<string, List<Line>> GetLayersWithLines(ImportInstance cadInstance)
        {
            var layersWithLines = new Dictionary<string, List<Line>>();

            try
            {
                var geoElement = cadInstance.get_Geometry(new Options());

                foreach (GeometryObject geoObj in geoElement)
                {
                    if (geoObj is GeometryInstance geoInstance)
                    {
                        var instanceGeometry = geoInstance.GetInstanceGeometry();
                        ProcessGeometryForLayers(instanceGeometry, layersWithLines, cadInstance.Document);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error reading CAD layers: " + ex.Message);
            }

            return layersWithLines;
        }

        /// <summary>
        /// Process geometry objects and organize by layer
        /// </summary>
        private static void ProcessGeometryForLayers(GeometryElement geometry, Dictionary<string, List<Line>> layersWithLines, Document doc)
        {
            foreach (GeometryObject geoObj in geometry)
            {
                if (geoObj is Line line)
                {
                    string layerName = GetLayerName(geoObj, doc);
                    AddLineToLayer(layersWithLines, layerName, line);
                }
                else if (geoObj is PolyLine polyLine)
                {
                    string layerName = GetLayerName(geoObj, doc);
                    // Convert polyline to individual line segments
                    var coordinates = polyLine.GetCoordinates();
                    for (int i = 0; i < coordinates.Count - 1; i++)
                    {
                        var segment = Line.CreateBound(coordinates[i], coordinates[i + 1]);
                        AddLineToLayer(layersWithLines, layerName, segment);
                    }
                }
                else if (geoObj is GeometryInstance nestedInstance)
                {
                    // Handle nested geometry instances
                    var nestedGeometry = nestedInstance.GetInstanceGeometry();
                    ProcessGeometryForLayers(nestedGeometry, layersWithLines, doc);
                }
            }
        }

        /// <summary>
        /// Get layer name from geometry object
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
                        // Debug output to see what we're getting
                        string styleName = graphicsStyle.Name;
                        System.Diagnostics.Debug.WriteLine($"Raw GraphicsStyle Name: '{styleName}'");

                        // Try to get the CAD layer name from the graphics style's category
                        if (graphicsStyle.GraphicsStyleCategory != null)
                        {
                            string categoryName = graphicsStyle.GraphicsStyleCategory.Name;
                            System.Diagnostics.Debug.WriteLine($"Category Name: '{categoryName}'");

                            // Often the category name contains the actual layer name
                            if (!string.IsNullOrWhiteSpace(categoryName))
                            {
                                // Remove common Revit prefixes from category names
                                string cleanCategoryName = categoryName;

                                // Remove prefixes like "Import Symbol - " or "CAD - "
                                string[] categoryPrefixes = { "Import Symbol - ", "CAD - ", "DWG - ", "<", ">" };
                                foreach (string prefix in categoryPrefixes)
                                {
                                    if (cleanCategoryName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                    {
                                        cleanCategoryName = cleanCategoryName.Substring(prefix.Length).Trim();
                                    }
                                    if (cleanCategoryName.EndsWith(prefix.Trim(), StringComparison.OrdinalIgnoreCase))
                                    {
                                        cleanCategoryName = cleanCategoryName.Substring(0, cleanCategoryName.Length - prefix.Trim().Length).Trim();
                                    }
                                }

                                // If we got a good category name, use it
                                if (!string.IsNullOrWhiteSpace(cleanCategoryName) &&
                                    !cleanCategoryName.All(char.IsDigit) &&
                                    cleanCategoryName != styleName)
                                {
                                    string finalCategoryName = CleanLayerName(cleanCategoryName);
                                    System.Diagnostics.Debug.WriteLine($"Using Category Name: '{finalCategoryName}'");
                                    return finalCategoryName;
                                }
                            }
                        }

                        // If category didn't work, try to extract layer name from the style name
                        string layerName = ExtractLayerNameFromStyle(styleName);
                        System.Diagnostics.Debug.WriteLine($"Final Layer Name: '{layerName}'");
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
        /// Extract the actual layer name from the graphics style name
        /// </summary>
        private static string ExtractLayerNameFromStyle(string styleName)
        {
            if (string.IsNullOrWhiteSpace(styleName))
                return "Unnamed Layer";

            // Try different patterns to extract the layer name
            string extracted = styleName;

            // Pattern 1: Remove angle brackets and content between them
            if (extracted.Contains("<") && extracted.Contains(">"))
            {
                // Extract content between < and >
                int startIndex = extracted.IndexOf('<') + 1;
                int endIndex = extracted.IndexOf('>');
                if (startIndex > 0 && endIndex > startIndex)
                {
                    string bracketContent = extracted.Substring(startIndex, endIndex - startIndex);
                    if (!string.IsNullOrWhiteSpace(bracketContent))
                    {
                        extracted = bracketContent;
                    }
                }
            }

            // Pattern 2: Remove "Import Symbol - " prefix
            if (extracted.StartsWith("Import Symbol - ", StringComparison.OrdinalIgnoreCase))
            {
                extracted = extracted.Substring("Import Symbol - ".Length);
            }

            // Pattern 3: If it looks like "Layer_123" or similar, try to find the actual name
            if (extracted.StartsWith("Layer_", StringComparison.OrdinalIgnoreCase) && extracted.Length > 6)
            {
                string afterUnderscore = extracted.Substring(6);
                // If what's after the underscore is not just numbers, it might be the layer name
                if (!afterUnderscore.All(char.IsDigit))
                {
                    extracted = afterUnderscore;
                }
            }

            // Pattern 4: Look for layer names in common CAD formats
            if (extracted.Contains("$"))
            {
                // Sometimes layer names are in format like "0$LAYERNAME"
                string[] parts = extracted.Split('$');
                if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    extracted = parts[1];
                }
            }

            // Clean up the extracted name
            return CleanLayerName(extracted);
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

            // If the name is still very technical or contains only special characters,
            // try to make it more readable
            if (IsUnfriendlyLayerName(cleaned))
            {
                cleaned = MakeLayerNameFriendly(cleaned);
            }

            return string.IsNullOrWhiteSpace(cleaned) ? "Unnamed Layer" : cleaned;
        }

        /// <summary>
        /// Check if a layer name is unfriendly (too technical or cryptic)
        /// </summary>
        private static bool IsUnfriendlyLayerName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return true;

            // Check if it's purely numeric
            if (name.All(char.IsDigit))
                return true;

            // Check if it's mostly special characters
            int specialCharCount = name.Count(c => !char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != ' ');
            if (specialCharCount > name.Length / 2)
                return true;

            // Check for common unfriendly patterns
            string[] unfriendlyPatterns = { "DEFPOINTS", "0", "*", "$", "~" };
            foreach (string pattern in unfriendlyPatterns)
            {
                if (name.Equals(pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Make a layer name more user-friendly
        /// </summary>
        private static string MakeLayerNameFriendly(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Unnamed Layer";

            // Handle common CAD layer naming conventions
            var friendlyMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "0", "Default Layer" },
                { "DEFPOINTS", "Reference Points" },
                { "A-PLAN", "Architecture Plan" },
                { "L-TREE", "Trees" },
                { "L-SHRB", "Shrubs" },
                { "L-PLNT", "Plants" },
                { "L-LAWN", "Lawn Areas" },
                { "L-PATH", "Pathways" },
                { "C-PROP", "Property Lines" },
                { "C-TOPO", "Topography" }
            };

            if (friendlyMappings.ContainsKey(name))
            {
                return friendlyMappings[name];
            }

            // Try to parse common naming patterns
            if (name.Contains("-"))
            {
                var parts = name.Split('-');
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
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower().Replace("_", " "));
        }

        /// <summary>
        /// Add line to the appropriate layer collection
        /// </summary>
        private static void AddLineToLayer(Dictionary<string, List<Line>> layersWithLines, string layerName, Line line)
        {
            if (!layersWithLines.ContainsKey(layerName))
            {
                layersWithLines[layerName] = new List<Line>();
            }
            layersWithLines[layerName].Add(line);
        }

        /// <summary>
        /// Get lines from specific layers
        /// </summary>
        public static List<Line> GetLinesFromSpecificLayers(ImportInstance cadInstance, List<string> selectedLayers)
        {
            var allLayers = GetLayersWithLines(cadInstance);
            var selectedLines = new List<Line>();

            foreach (var layerName in selectedLayers)
            {
                if (allLayers.ContainsKey(layerName))
                {
                    selectedLines.AddRange(allLayers[layerName]);
                }
            }

            return selectedLines;
        }

        /// <summary>
        /// Get layer name by ID - for compatibility with the TreesWindow update
        /// </summary>
        public static string GetLayerNameById(ImportInstance cadFile, string layerId)
        {
            try
            {
                var doc = cadFile.Document;
                if (int.TryParse(layerId.Replace("Layer_", ""), out int elementIdValue))
                {
                    var elementId = new ElementId(elementIdValue);
                    var graphicsStyle = doc.GetElement(elementId) as GraphicsStyle;
                    if (graphicsStyle != null)
                    {
                        return CleanLayerName(graphicsStyle.Name);
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get layer description - for compatibility with the TreesWindow update
        /// </summary>
        public static string GetLayerDescription(ImportInstance cadFile, string layerKey)
        {
            // For now, return null as CAD layers typically don't have separate descriptions
            // This could be expanded if your CAD files have additional metadata
            return null;
        }

        /// <summary>
        /// Get layer display name - for compatibility with the TreesWindow update
        /// </summary>
        public static string GetLayerDisplayName(ImportInstance cadFile, string layerKey)
        {
            // Try to get a more descriptive name
            return CleanLayerName(layerKey);
        }
    }
}