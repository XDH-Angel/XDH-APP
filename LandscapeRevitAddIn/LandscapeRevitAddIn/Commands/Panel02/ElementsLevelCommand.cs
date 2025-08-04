using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using LandscapeRevitAddIn.UI.Windows.Panel02;
using LandscapeRevitAddIn.Models;

namespace LandscapeRevitAddIn.Commands.Panel02
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ElementsLevelCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                // Get selected elements
                var selectedElements = uiDoc.Selection.GetElementIds()
                    .Select(id => doc.GetElement(id))
                    .Where(e => e != null)
                    .ToList();

                if (selectedElements.Count == 0)
                {
                    TaskDialog.Show("No Selection", "Please select one or more elements before running this command.");
                    return Result.Failed;
                }

                // Launch the level adjustment window
                var window = new LevelAdjustmentWindow(doc, selectedElements, "Elements");
                bool? dialogResult = window.ShowDialog();

                if (dialogResult == true)
                {
                    var data = window.GetAdjustmentData();

                    using (Transaction trans = new Transaction(doc, "Adjust Element Levels"))
                    {
                        trans.Start();

                        foreach (var element in selectedElements)
                        {
                            // Set Level
                            var levelParam = element.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)
                                ?? element.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM)
                                ?? element.get_Parameter(BuiltInParameter.LEVEL_PARAM)
                                ?? element.get_Parameter(BuiltInParameter.SCHEDULE_LEVEL_PARAM);

                            if (levelParam != null && levelParam.HasValue && data.TargetLevel != null)
                            {
                                levelParam.Set(data.TargetLevel.Id);
                            }

                            // Set Elevation Offset (if applicable)
                            if (data.AdjustElevation)
                            {
                                var offsetParam = element.LookupParameter("Offset")
                                    ?? element.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);

                                if (offsetParam != null && offsetParam.StorageType == StorageType.Double)
                                {
                                    double internalOffset = UnitUtils.ConvertToInternalUnits(
                                        data.ElevationOffset, UnitTypeId.Feet);
                                    offsetParam.Set(internalOffset);
                                }
                            }
                        }

                        trans.Commit();
                    }

                    TaskDialog.Show("Success", "Element levels have been adjusted.");
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Error", $"An unexpected error occurred:\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}
