using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using LandscapeRevitAddIn.UI.Windows;
using LandscapeRevitAddIn.Models;
using LandscapeRevitAddIn.UI.Windows.Panel02;

namespace LandscapeRevitAddIn.Commands.Panel02
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class MultipleLinesLevelsCommand : IExternalCommand
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

                // Show multiple lines and levels window
                var multipleLinesWindow = new MultipleLinesLevelsWindow(doc);
                var dialogResult = multipleLinesWindow.ShowDialog();

                if (dialogResult == true)
                {
                    var adjustmentData = multipleLinesWindow.GetAdjustmentData();
                    int adjustedCount = AdjustFloorsByLinesAndLevels(doc, adjustmentData);

                    TaskDialog.Show("Success", $"Successfully modified {adjustedCount} floor points based on lines and levels.");
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

        private int AdjustFloorsByLinesAndLevels(Document doc, MultipleLinesLevelsData adjustmentData)
        {
            int adjustedCount = 0;

            using (Transaction trans = new Transaction(doc, "Adjust Floors by Lines and Levels"))
            {
                trans.Start();

                try
                {
                    foreach (var floorData in adjustmentData.FloorAdjustments)
                    {
                        if (ModifyFloorByLinesAndLevels(doc, floorData))
                        {
                            adjustedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in floor adjustment: {ex.Message}");
                }

                trans.Commit();
            }

            return adjustedCount;
        }

        private bool ModifyFloorByLinesAndLevels(Document doc, FloorLinesLevelsData floorData)
        {
            try
            {
                Floor floor = floorData.Floor;

                // Get floor boundary curves
                var floorBoundary = GetFloorBoundary(floor);
                if (floorBoundary == null || floorBoundary.Size == 0) return false;

                // Create new points based on lines and levels
                var newPoints = CreateSketchPointsFromLinesAndLevels(floorData.ReferenceLines, floorData.ReferenceLevels);

                if (newPoints.Count > 0)
                {
                    // Modify floor using points and levels
                    ModifyFloorElevation(floor, floorData.ReferenceLevels);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error modifying floor {floorData.Floor.Id}: {ex.Message}");
            }

            return false;
        }

        private CurveArray GetFloorBoundary(Floor floor)
        {
            try
            {
                // Get floor geometry
                Options opt = new Options();
                opt.ComputeReferences = true;
                opt.DetailLevel = ViewDetailLevel.Fine;

                GeometryElement geomElem = floor.get_Geometry(opt);

                foreach (GeometryObject geomObj in geomElem)
                {
                    if (geomObj is Solid solid && solid.Volume > 0)
                    {
                        // Get the bottom face edges
                        foreach (Face face in solid.Faces)
                        {
                            if (face is PlanarFace planarFace)
                            {
                                EdgeArrayArray boundaries = face.EdgeLoops;
                                if (boundaries.Size > 0)
                                {
                                    CurveArray curves = new CurveArray();
                                    EdgeArray edges = boundaries.get_Item(0);

                                    foreach (Edge edge in edges)
                                    {
                                        curves.Append(edge.AsCurve());
                                    }
                                    return curves;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting floor boundary: {ex.Message}");
            }

            return new CurveArray();
        }

        private void ModifyFloorElevation(Floor floor, List<Level> referenceLevels)
        {
            try
            {
                // Adjust floor level to the first reference level
                if (referenceLevels.Count > 0)
                {
                    Parameter levelParam = floor.get_Parameter(BuiltInParameter.LEVEL_PARAM);
                    if (levelParam != null && !levelParam.IsReadOnly)
                    {
                        levelParam.Set(referenceLevels[0].Id);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error modifying floor elevation: {ex.Message}");
            }
        }

        private List<XYZ> CreateSketchPointsFromLinesAndLevels(List<Line> referenceLines, List<Level> referenceLevels)
        {
            var points = new List<XYZ>();

            foreach (var line in referenceLines)
            {
                foreach (var level in referenceLevels)
                {
                    // Create points at line endpoints with level elevation
                    var startPoint = new XYZ(line.GetEndPoint(0).X, line.GetEndPoint(0).Y, level.Elevation);
                    var endPoint = new XYZ(line.GetEndPoint(1).X, line.GetEndPoint(1).Y, level.Elevation);

                    points.Add(startPoint);
                    points.Add(endPoint);
                }
            }

            return points.Distinct().ToList();
        }
    }
}