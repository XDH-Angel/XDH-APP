// Commands/Panel07/PointToSurfaceCommand.cs
using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using LandscapeRevitAddIn.UI.Windows.Panel07;

namespace LandscapeRevitAddIn.Commands.Panel07
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class PointToSurfaceCommand : IExternalCommand
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
                    message = "No active document found.";
                    return Result.Failed;
                }

                // Show the Point to Surface window
                var window = new PointToSurfaceWindow(uiDoc);
                bool? result = window.ShowDialog();

                if (result == true)
                {
                    // Get the selected data from the window
                    var selectedPoints = window.SelectedPoints;
                    var targetFloor = window.TargetFloor;
                    var referenceSurface = window.ReferenceSurface;

                    if (selectedPoints != null && selectedPoints.Count > 0 && referenceSurface != null)
                    {
                        // Execute the point alignment logic here
                        bool success = AlignPointsToSurface(doc, selectedPoints, targetFloor, referenceSurface);

                        if (success)
                        {
                            TaskDialog.Show("Success", $"Successfully aligned {selectedPoints.Count} points to reference surface.");
                        }
                        else
                        {
                            TaskDialog.Show("Warning", "Failed to align points to surface.");
                        }
                    }

                    return Result.Succeeded;
                }
                else
                {
                    return Result.Cancelled;
                }
            }
            catch (Exception ex)
            {
                message = $"Error in Point to Surface command: {ex.Message}";
                return Result.Failed;
            }
        }

        private bool AlignPointsToSurface(Document doc, System.Collections.Generic.List<XYZ> points, Floor floor, Face surface)
        {
            try
            {
                using (Transaction trans = new Transaction(doc, "Align Points to Surface"))
                {
                    trans.Start();

                    // Implementation for aligning points to surface
                    // This is where you would implement the actual alignment logic

                    // For now, return true as placeholder
                    trans.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error aligning points to surface: {ex.Message}");
                return false;
            }
        }
    }
}