// Commands/Panel05/SlopeValueCommand.cs
using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using LandscapeRevitAddIn.UI.Windows.Panel05;
using LandscapeRevitAddIn.Utils;

namespace LandscapeRevitAddIn.Commands.Panel05
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class SlopeValueCommand : IExternalCommand
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

                var slopeValueWindow = new SlopeValueWindow(doc);
                var dialogResult = slopeValueWindow.ShowDialog();

                if (dialogResult == true)
                {
                    var selectedFloor = slopeValueWindow.SelectedFloor;
                    var slopeDirection = slopeValueWindow.SlopeDirection;
                    var slopeValue = slopeValueWindow.SlopeValue;
                    var isPercentage = slopeValueWindow.IsPercentage;

                    if (selectedFloor != null && slopeValue > 0)
                    {
                        var result = ApplySlopeToFloor(doc, selectedFloor, slopeDirection, slopeValue, isPercentage);
                        if (result)
                        {
                            var unit = isPercentage ? "%" : "°";
                            TaskDialog.Show("Success", $"Slope of {slopeValue}{unit} applied to floor successfully.");
                        }
                        else
                        {
                            TaskDialog.Show("Error", "Failed to apply slope to floor.");
                        }
                    }
                    else
                    {
                        TaskDialog.Show("No Selection", "Please select a floor and enter a valid slope value.");
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

        private bool ApplySlopeToFloor(Document doc, Floor floor, XYZ direction, double slopeValue, bool isPercentage)
        {
            try
            {
                using (Transaction trans = new Transaction(doc, "Apply Slope to Floor"))
                {
                    trans.Start();

                    // Convert percentage to angle if needed
                    double slopeAngle = isPercentage ? Math.Atan(slopeValue / 100.0) : slopeValue * Math.PI / 180.0;

                    // FIXED: Changed P5FloorUtils to PSFloorUtils (our created utility class)
                    bool success = PSFloorUtils.ApplySlopeToFloor(doc, floor, direction, slopeAngle);

                    if (success)
                    {
                        trans.Commit();
                        return true;
                    }
                    else
                    {
                        trans.RollBack();
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Error applying slope: {ex.Message}");
                return false;
            }
        }
    }
}