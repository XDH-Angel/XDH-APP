using System;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using LandscapeRevitAddIn.UI.Windows;
using LandscapeRevitAddIn.UI.Windows.Panel08;

namespace LandscapeRevitAddIn.Commands.Panel08
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class FilledRegionByFaceCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Show the professional WPF window
                var window = new FilledRegionByFaceWindow(uidoc);
                bool? result = window.ShowDialog();

                if (result == true)
                {
                    return Result.Succeeded;
                }
                else
                {
                    return Result.Cancelled;  
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}