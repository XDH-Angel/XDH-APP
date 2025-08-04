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
    public class FloorsLevelCommand : IExternalCommand
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

                // Get selected floors
                var selection = uiDoc.Selection;
                var selectedIds = selection.GetElementIds();

                var floors = GetSelectedFloors(doc, selectedIds);

                if (floors.Count == 0)
                {
                    TaskDialog.Show("No Floors Selected", "Please select floors to adjust their levels.");
                    return Result.Cancelled;
                }

                // Show level adjustment window
                var levelWindow = new LevelAdjustmentWindow(doc, floors.Cast<Element>().ToList(), "Floors");
                var dialogResult = levelWindow.ShowDialog();

                if (dialogResult == true)
                {
                    var adjustmentData = levelWindow.GetAdjustmentData();
                    int adjustedCount = AdjustFloorLevels(doc, floors, adjustmentData);

                    TaskDialog.Show("Success", $"Successfully adjusted levels for {adjustedCount} floors.");
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

        private List<Floor> GetSelectedFloors(Document doc, ICollection<ElementId> elementIds)
        {
            var floors = new List<Floor>();

            foreach (ElementId id in elementIds)
            {
                Element element = doc.GetElement(id);
                if (element is Floor floor)
                {
                    floors.Add(floor);
                }
            }

            return floors;
        }

        private int AdjustFloorLevels(Document doc, List<Floor> floors, LevelAdjustmentData adjustmentData)
        {
            int adjustedCount = 0;

            using (Transaction trans = new Transaction(doc, "Adjust Floor Levels"))
            {
                trans.Start();

                foreach (Floor floor in floors)
                {
                    try
                    {
                        if (AdjustFloorLevel(floor, adjustmentData))
                        {
                            adjustedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error adjusting floor {floor.Id}: {ex.Message}");
                    }
                }

                trans.Commit();
            }

            return adjustedCount;
        }

        private bool AdjustFloorLevel(Floor floor, LevelAdjustmentData adjustmentData)
        {
            // Adjust floor level
            Parameter levelParam = floor.get_Parameter(BuiltInParameter.LEVEL_PARAM);
            if (levelParam != null && !levelParam.IsReadOnly)
            {
                levelParam.Set(adjustmentData.TargetLevel.Id);

                // Adjust height offset if specified
                if (adjustmentData.AdjustElevation && Math.Abs(adjustmentData.ElevationOffset) > 1e-6)
                {
                    Parameter offsetParam = floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
                    if (offsetParam != null && !offsetParam.IsReadOnly)
                    {
                        double currentOffset = offsetParam.AsDouble();
                        offsetParam.Set(currentOffset + adjustmentData.ElevationOffset);
                    }
                }

                return true;
            }

            return false;
        }
    }
}