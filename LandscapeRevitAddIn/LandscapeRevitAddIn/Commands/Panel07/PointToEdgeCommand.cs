// Commands/Panel07/PointToEdgeCommand.cs
using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using LandscapeRevitAddIn.UI.Windows.Panel07;

namespace LandscapeRevitAddIn.Commands.Panel07
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class PointToEdgeCommand : IExternalCommand
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

                // Show the Point to Edge window
                var window = new PointToEdgeWindow(uiDoc);
                bool? result = window.ShowDialog();

                if (result == true)
                {
                    TaskDialog.Show("Success", "Point alignment completed successfully.");
                    return Result.Succeeded;
                }
                else
                {
                    return Result.Cancelled;
                }
            }
            catch (Exception ex)
            {
                message = $"Error in Point to Edge command: {ex.Message}";
                return Result.Failed;
            }
        }
    }
}