// Commands/Panel05/SurfaceCommand.cs
using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using LandscapeRevitAddIn.Utils;

namespace LandscapeRevitAddIn.Commands.Panel05
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class SurfaceCommand : IExternalCommand
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

                // Step 1: Select floor to adjust
                Floor selectedFloor = null;
                try
                {
                    TaskDialog.Show("Select Floor", "Please select the floor to adjust.");
                    var floorRef = uiDoc.Selection.PickObject(ObjectType.Element, new FloorSelectionFilter(), "Select floor to adjust");
                    selectedFloor = doc.GetElement(floorRef) as Floor;
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }

                if (selectedFloor == null)
                {
                    TaskDialog.Show("Error", "Selected element is not a floor.");
                    return Result.Failed;
                }

                // Step 2: Select reference face
                Face referenceFace = null;
                try
                {
                    TaskDialog.Show("Select Reference Face", "Please select the reference face to project from.");
                    var faceRef = uiDoc.Selection.PickObject(ObjectType.Face, "Select reference face");
                    var element = doc.GetElement(faceRef);
                    var geometryObject = element.GetGeometryObjectFromReference(faceRef);
                    referenceFace = geometryObject as Face;
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    return Result.Cancelled;
                }

                if (referenceFace == null)
                {
                    TaskDialog.Show("Error", "Failed to get reference face.");
                    return Result.Failed;
                }

                // Step 3: Apply surface projection to floor
                var result = ProjectSurfaceToFloor(doc, selectedFloor, referenceFace);
                if (result)
                {
                    TaskDialog.Show("Success", "Floor adjusted successfully using reference surface.");
                }
                else
                {
                    TaskDialog.Show("Error", "Failed to adjust floor using reference surface.");
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

        private bool ProjectSurfaceToFloor(Document doc, Floor floor, Face referenceFace)
        {
            try
            {
                using (Transaction trans = new Transaction(doc, "Project Surface to Floor"))
                {
                    trans.Start();

                    bool success = P5FloorUtils.AdjustFloorByReferenceSurface(doc, floor, referenceFace);

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
                TaskDialog.Show("Error", $"Error projecting surface: {ex.Message}");
                return false;
            }
        }
    }

    public class FloorSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Floor;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}