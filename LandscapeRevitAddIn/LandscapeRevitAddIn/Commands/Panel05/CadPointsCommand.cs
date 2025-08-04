// Commands/Panel05/CadPointsCommand.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using LandscapeRevitAddIn.UI.Windows.Panel05;
using LandscapeRevitAddIn.Utils;

namespace LandscapeRevitAddIn.Commands.Panel05
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CadPointsCommand : IExternalCommand
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

                var cadPointsWindow = new CadPointsWindow(doc);
                var dialogResult = cadPointsWindow.ShowDialog();

                if (dialogResult == true)
                {
                    var selectedLayer = cadPointsWindow.SelectedLayer;
                    var selectedFloor = cadPointsWindow.SelectedFloor;
                    var cadFile = cadPointsWindow.SelectedCADFile;

                    if (!string.IsNullOrEmpty(selectedLayer) && selectedFloor != null && cadFile != null)
                    {
                        var result = AdjustFloorByCadPoints(doc, cadFile, selectedLayer, selectedFloor);
                        if (result)
                        {
                            TaskDialog.Show("Success", "Floor adjusted successfully using CAD points.");
                        }
                        else
                        {
                            TaskDialog.Show("Error", "Failed to adjust floor using CAD points.");
                        }
                    }
                    else
                    {
                        TaskDialog.Show("No Selection", "Please select CAD layer and floor.");
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

        private bool AdjustFloorByCadPoints(Document doc, ImportInstance cadFile, string layerName, Floor floor)
        {
            try
            {
                using (Transaction trans = new Transaction(doc, "Adjust Floor by CAD Points"))
                {
                    trans.Start();

                    // FIXED: Changed P5CADUtils to PSCADUtils (our created utility class)
                    var cadPoints = PSCADUtils.GetPointsFromLayer(doc, layerName);

                    if (cadPoints.Count == 0)
                    {
                        TaskDialog.Show("Warning", "No points found on the selected CAD layer.");
                        trans.RollBack();
                        return false;
                    }

                    // FIXED: Changed P5FloorUtils to PSFloorUtils (our created utility class)
                    bool success = PSFloorUtils.AdjustFloorBySurfacePoints(doc, floor, cadPoints);

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
                TaskDialog.Show("Error", $"Error adjusting floor: {ex.Message}");
                return false;
            }
        }
    }
}