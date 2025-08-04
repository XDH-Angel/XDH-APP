using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace LandscapeRevitAddIn.Utils
{
    public static class RevitUtils
    {
        public static Level GetDefaultLevel(Document doc)
        {
            try
            {
                // Get all levels and return the first one (typically Level 1)
                var levels = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .OrderBy(l => l.Elevation)
                    .ToList();

                return levels.FirstOrDefault();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting default level: {ex.Message}");
                return null;
            }
        }

        public static List<Level> GetAllLevels(Document doc)
        {
            try
            {
                return new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .OrderBy(l => l.Elevation)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting levels: {ex.Message}");
                return new List<Level>();
            }
        }

        public static XYZ GetElementLocation(Element element)
        {
            try
            {
                if (element.Location is LocationPoint locationPoint)
                {
                    return locationPoint.Point;
                }
                else if (element.Location is LocationCurve locationCurve)
                {
                    // Return midpoint of the curve
                    Curve curve = locationCurve.Curve;
                    return curve.Evaluate(0.5, true);
                }

                // If no location, try to get from bounding box
                BoundingBoxXYZ bbox = element.get_BoundingBox(null);
                if (bbox != null)
                {
                    return (bbox.Min + bbox.Max) / 2;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting element location: {ex.Message}");
            }

            return XYZ.Zero;
        }

        public static Element GetElementFromReference(Document doc, Reference reference)
        {
            try
            {
                // FIXED LINE 80 - Get element from reference
                if (reference != null && reference.ElementId != ElementId.InvalidElementId)
                {
                    return doc.GetElement(reference.ElementId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting element from reference: {ex.Message}");
            }

            return null;
        }

        public static GeometryObject GetGeometryFromReference(Document doc, Reference reference)
        {
            try
            {
                Element element = GetElementFromReference(doc, reference);
                if (element != null)
                {
                    Options options = new Options();
                    GeometryElement geomElement = element.get_Geometry(options);

                    foreach (GeometryObject geomObj in geomElement)
                    {
                        return geomObj; // Return first geometry object
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting geometry from reference: {ex.Message}");
            }

            return null;
        }

        public static bool IsElementVisible(Element element, View view)
        {
            try
            {
                if (element == null || view == null) return false;

                // Check if element is hidden in view
                if (element.IsHidden(view)) return false;

                // Check if element category is visible in view
                Category category = element.Category;
                if (category != null)
                {
                    return !view.GetCategoryHidden(category.Id);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking element visibility: {ex.Message}");
                return false;
            }
        }

        public static string GetElementDescription(Element element)
        {
            try
            {
                if (element == null) return "Unknown Element";

                string typeName = element.GetType().Name;
                string name = element.Name ?? "Unnamed";
                string category = element.Category?.Name ?? "Unknown Category";

                return $"{typeName}: {name} ({category}) - ID: {element.Id}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting element description: {ex.Message}");
                return $"Element ID: {element?.Id ?? ElementId.InvalidElementId}";
            }
        }

        public static void ShowMessage(string title, string message)
        {
            try
            {
                TaskDialog dialog = new TaskDialog(title);
                dialog.MainContent = message;
                dialog.Show();
            }
            catch
            {
                // Fallback to simple message if TaskDialog fails
                System.Windows.MessageBox.Show(message, title);
            }
        }

        public static T GetParameterValue<T>(Element element, BuiltInParameter parameterName, T defaultValue = default(T))
        {
            try
            {
                Parameter parameter = element.get_Parameter(parameterName);
                if (parameter != null && parameter.HasValue)
                {
                    if (typeof(T) == typeof(string))
                    {
                        return (T)(object)parameter.AsString();
                    }
                    else if (typeof(T) == typeof(double))
                    {
                        return (T)(object)parameter.AsDouble();
                    }
                    else if (typeof(T) == typeof(int))
                    {
                        return (T)(object)parameter.AsInteger();
                    }
                    else if (typeof(T) == typeof(ElementId))
                    {
                        return (T)(object)parameter.AsElementId();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting parameter value: {ex.Message}");
            }

            return defaultValue;
        }
    }
}