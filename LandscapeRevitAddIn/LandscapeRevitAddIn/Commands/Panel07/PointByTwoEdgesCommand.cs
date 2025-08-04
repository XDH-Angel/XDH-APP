// Commands/Panel07/PointByTwoEdgesCommand.cs
using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using LandscapeRevitAddIn.UI.Windows.Panel07;

namespace LandscapeRevitAddIn.Commands.Panel07
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class PointByTwoEdgesCommand : IExternalCommand
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

                // Show the Point by Two Edges window
                var window = new PointByTwoEdgesWindow(uiDoc);
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
                message = $"Error in Point by Two Edges command: {ex.Message}";
                return Result.Failed;
            }
        }
    }
}