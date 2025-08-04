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
    public class WallsLevelCommand : IExternalCommand
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

                // Get selected walls
                var selection = uiDoc.Selection;
                var selectedIds = selection.GetElementIds();

                var walls = GetSelectedWalls(doc, selectedIds);

                if (walls.Count == 0)
                {
                    TaskDialog.Show("No Walls Selected", "Please select walls to adjust their levels and heights.");
                    return Result.Cancelled;
                }

                // Show wall adjustment window
                var wallWindow = new LandscapeRevitAddIn.UI.Windows.Panel02.WallAdjustmentWindow(doc, walls);
                var dialogResult = wallWindow.ShowDialog();

                if (dialogResult == true)
                {
                    var adjustmentData = wallWindow.GetAdjustmentData();
                    int adjustedCount = AdjustWallLevels(doc, walls, adjustmentData);

                    TaskDialog.Show("Success", $"Successfully adjusted {adjustedCount} walls.");
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

        private List<Wall> GetSelectedWalls(Document doc, ICollection<ElementId> elementIds)
        {
            var walls = new List<Wall>();

            foreach (ElementId id in elementIds)
            {
                Element element = doc.GetElement(id);
                if (element is Wall wall)
                {
                    walls.Add(wall);
                }
            }

            return walls;
        }

        private int AdjustWallLevels(Document doc, List<Wall> walls, WallAdjustmentData adjustmentData)
        {
            int adjustedCount = 0;

            using (Transaction trans = new Transaction(doc, "Adjust Wall Levels and Heights"))
            {
                trans.Start();

                foreach (Wall wall in walls)
                {
                    try
                    {
                        if (AdjustWallLevel(wall, adjustmentData))
                        {
                            adjustedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error adjusting wall {wall.Id}: {ex.Message}");
                    }
                }

                trans.Commit();
            }

            return adjustedCount;
        }

        private bool AdjustWallLevel(Wall wall, WallAdjustmentData adjustmentData)
        {
            bool success = false;

            // Adjust base level
            if (adjustmentData.AdjustBaseLevel)
            {
                Parameter baseLevelParam = wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
                if (baseLevelParam != null && !baseLevelParam.IsReadOnly)
                {
                    baseLevelParam.Set(adjustmentData.BaseLevel.Id);
                    success = true;

                    // Adjust base offset
                    if (Math.Abs(adjustmentData.BaseOffset) > 1e-6)
                    {
                        Parameter baseOffsetParam = wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
                        if (baseOffsetParam != null && !baseOffsetParam.IsReadOnly)
                        {
                            double currentOffset = baseOffsetParam.AsDouble();
                            baseOffsetParam.Set(currentOffset + adjustmentData.BaseOffset);
                        }
                    }
                }
            }

            // Adjust top level
            if (adjustmentData.AdjustTopLevel)
            {
                Parameter topLevelParam = wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
                if (topLevelParam != null && !topLevelParam.IsReadOnly)
                {
                    topLevelParam.Set(adjustmentData.TopLevel.Id);
                    success = true;

                    // Adjust top offset
                    if (Math.Abs(adjustmentData.TopOffset) > 1e-6)
                    {
                        Parameter topOffsetParam = wall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET);
                        if (topOffsetParam != null && !topOffsetParam.IsReadOnly)
                        {
                            double currentOffset = topOffsetParam.AsDouble();
                            topOffsetParam.Set(currentOffset + adjustmentData.TopOffset);
                        }
                    }
                }
            }

            // Adjust unconnected height
            if (adjustmentData.AdjustHeight && Math.Abs(adjustmentData.HeightAdjustment) > 1e-6)
            {
                Parameter heightParam = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
                if (heightParam != null && !heightParam.IsReadOnly)
                {
                    double currentHeight = heightParam.AsDouble();
                    heightParam.Set(currentHeight + adjustmentData.HeightAdjustment);
                    success = true;
                }
            }

            return success;
        }
    }
}