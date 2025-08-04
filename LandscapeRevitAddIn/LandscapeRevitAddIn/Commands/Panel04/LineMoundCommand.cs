using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using LandscapeRevitAddIn.UI.Windows.Panel04;

namespace LandscapeRevitAddIn.Commands.Panel04
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class LineMoundCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                if (doc == null)
                {
                    TaskDialog.Show("Error", "No active document found.");
                    return Result.Failed;
                }

                // Show the LineMound window
                var lineMoundWindow = new LineMoundWindow(doc);
                var dialogResult = lineMoundWindow.ShowDialog();

                if (dialogResult == true)
                {
                    var lineLevelMappings = lineMoundWindow.GetValidMappings();

                    if (lineLevelMappings.Any())
                    {
                        var result = CreateMoundsFromLines(doc, lineLevelMappings);

                        if (result)
                        {
                            TaskDialog.Show("Success",
                                $"Mounds created successfully from {lineLevelMappings.Count()} line-level mapping(s).");
                        }
                        else
                        {
                            TaskDialog.Show("Error", "Failed to create mounds. Please check the selected lines and levels.");
                        }
                    }
                    else
                    {
                        TaskDialog.Show("No Mappings", "Please configure line-to-level mappings.");
                    }
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Error", $"An error occurred: {ex.Message}");
                return Result.Failed;
            }
        }

        private bool CreateMoundsFromLines(Document doc, IEnumerable<LineLevelMapping> mappings)
        {
            try
            {
                using (Transaction trans = new Transaction(doc, "Create Line Mounds"))
                {
                    trans.Start();

                    foreach (var mapping in mappings)
                    {
                        if (mapping.SelectedLines != null && mapping.SelectedLines.Any() && mapping.Level != null)
                        {
                            CreateMoundFromLineGroup(doc, mapping.SelectedLines, mapping.Level, mapping.Elevation);
                        }
                    }

                    trans.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating mounds: {ex.Message}");
                return false;
            }
        }

        private void CreateMoundFromLineGroup(Document doc, IEnumerable<Curve> lines, Level level, double elevation)
        {
            try
            {
                // Create points from line endpoints and midpoints
                var points = new List<XYZ>();

                foreach (var line in lines)
                {
                    // Add start point
                    var startPoint = new XYZ(line.GetEndPoint(0).X, line.GetEndPoint(0).Y, elevation);
                    points.Add(startPoint);

                    // Add end point
                    var endPoint = new XYZ(line.GetEndPoint(1).X, line.GetEndPoint(1).Y, elevation);
                    points.Add(endPoint);

                    // Add midpoint for better surface definition
                    var midPoint = line.Evaluate(0.5, true);
                    var elevatedMidPoint = new XYZ(midPoint.X, midPoint.Y, elevation);
                    points.Add(elevatedMidPoint);
                }

                // Remove duplicate points
                points = RemoveDuplicatePoints(points);

                if (points.Count >= 3)
                {
                    // Create topography from points
                    TopographySurface.Create(doc, points);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating mound from line group: {ex.Message}");
            }
        }

        private List<XYZ> RemoveDuplicatePoints(List<XYZ> points)
        {
            var uniquePoints = new List<XYZ>();
            const double tolerance = 0.01; // 1 cm tolerance

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

    // Model class for line-level mapping
    public class LineLevelMapping
    {
        public IEnumerable<Curve> SelectedLines { get; set; }
        public Level Level { get; set; }
        public double Elevation { get; set; }
        public string Description { get; set; }
    }
}