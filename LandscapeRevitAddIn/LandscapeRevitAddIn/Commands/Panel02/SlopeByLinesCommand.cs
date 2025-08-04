using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using LandscapeRevitAddIn.Models;
using LandscapeRevitAddIn.UI.Windows;
using LandscapeRevitAddIn.UI.Windows.Panel02;

namespace LandscapeRevitAddIn.Commands.Panel02
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class SlopeByLinesCommand : IExternalCommand
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

                // Show slope by lines window
                var slopeWindow = new SlopeByLinesWindow(doc);
                var dialogResult = slopeWindow.ShowDialog();

                if (dialogResult == true)
                {
                    var slopeData = slopeWindow.GetSlopeData();
                    int createdCount = CreateSlopeElements(doc, slopeData);

                    TaskDialog.Show("Success", $"Successfully created slope elements for {createdCount} lines.");
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

        private int CreateSlopeElements(Document doc, SlopeByLinesData slopeData)
        {
            int createdCount = 0;

            using (Transaction trans = new Transaction(doc, "Create Slope by Lines"))
            {
                trans.Start();

                foreach (var lineData in slopeData.LineSlopes)
                {
                    try
                    {
                        if (CreateSlopeFromLine(doc, lineData))
                        {
                            createdCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error creating slope from line: {ex.Message}");
                    }
                }

                trans.Commit();
            }

            return createdCount;
        }

        private bool CreateSlopeFromLine(Document doc, LineSlopeData lineData)
        {
            try
            {
                Line line = lineData.ReferenceLine;
                double slopePercentage = lineData.SlopePercentage;

                // If no line provided (demo mode), create a sample line
                if (line == null)
                {
                    // Create a sample horizontal line for demonstration
                    XYZ start = new XYZ(0, 0, lineData.TargetLevel.Elevation);
                    XYZ end = new XYZ(50, 0, lineData.TargetLevel.Elevation);
                    line = Line.CreateBound(start, end);
                }

                // Calculate slope points with elevation changes
                var points = CalculateSlopePoints(line, slopePercentage);

                if (points.Count >= 2)
                {
                    // Create model lines or other geometric representation
                    CreateSlopeGeometry(doc, points, lineData.TargetLevel);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CreateSlopeFromLine: {ex.Message}");
            }

            return false;
        }

        private List<XYZ> CalculateSlopePoints(Line line, double slopePercentage)
        {
            var points = new List<XYZ>();

            XYZ start = line.GetEndPoint(0);
            XYZ end = line.GetEndPoint(1);

            double lineLength = line.Length;
            double elevationChange = lineLength * (slopePercentage / 100.0);

            // Create points along the line with elevation changes
            int numPoints = Math.Max(10, (int)(lineLength / 5.0)); // Point every 5 feet or minimum 10 points

            for (int i = 0; i <= numPoints; i++)
            {
                double parameter = (double)i / numPoints;
                XYZ point = start.Add((end.Subtract(start)).Multiply(parameter));

                // Add elevation based on slope
                double elevation = start.Z + (elevationChange * parameter);
                points.Add(new XYZ(point.X, point.Y, elevation));
            }

            return points;
        }

        private void CreateSlopeGeometry(Document doc, List<XYZ> points, Level targetLevel)
        {
            try
            {
                // Create model lines to represent the slope
                SketchPlane sketchPlane = SketchPlane.Create(doc, targetLevel.GetPlaneReference());

                for (int i = 0; i < points.Count - 1; i++)
                {
                    try
                    {
                        Line line = Line.CreateBound(points[i], points[i + 1]);
                        ModelCurve modelLine = doc.Create.NewModelCurve(line, sketchPlane);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error creating model line: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating slope geometry: {ex.Message}");
            }
        }
    }
}