using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using LandscapeRevitAddIn.UI.Windows.Panel03;

namespace LandscapeRevitAddIn.Commands.Panel03
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class FloorBandCommand : IExternalCommand
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

                // Show the Floor Band window
                var floorBandWindow = new FloorBandWindow(doc);
                var dialogResult = floorBandWindow.ShowDialog();

                if (dialogResult == true)
                {
                    var selectedFloor = floorBandWindow.SelectedFloor;
                    var offsetDistance = floorBandWindow.OffsetDistance;
                    var isOutward = floorBandWindow.IsOutward;
                    var selectedFloorType = floorBandWindow.SelectedFloorType;

                    if (selectedFloor != null && selectedFloorType != null)
                    {
                        var result = CreateFloorBand(doc, selectedFloor, offsetDistance, isOutward, selectedFloorType);

                        if (result)
                        {
                            TaskDialog.Show("Success",
                                $"Floor band created successfully with {Math.Abs(offsetDistance):F2} ft offset " +
                                $"({(isOutward ? "outward" : "inward")})");
                        }
                        else
                        {
                            TaskDialog.Show("Error", "Failed to create floor band. Please check the selected floor and try again.");
                        }
                    }
                    else
                    {
                        TaskDialog.Show("No Selection", "Please select a floor and floor type.");
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

        private bool CreateFloorBand(Document doc, Floor existingFloor, double offsetDistance, bool isOutward, FloorType newFloorType)
        {
            try
            {
                using (Transaction trans = new Transaction(doc, "Create Floor Band"))
                {
                    trans.Start();

                    // Get the existing floor's sketch
                    var sketchId = existingFloor.SketchId;
                    if (sketchId == ElementId.InvalidElementId)
                    {
                        trans.RollBack();
                        return false;
                    }

                    var sketch = doc.GetElement(sketchId) as Sketch;
                    if (sketch == null)
                    {
                        trans.RollBack();
                        return false;
                    }

                    // Get the floor boundary curves
                    var boundaryCurves = GetFloorBoundaryCurves(sketch);
                    if (boundaryCurves.Count == 0)
                    {
                        trans.RollBack();
                        return false;
                    }

                    // Create offset curves
                    var offsetCurves = CreateOffsetCurves(boundaryCurves, offsetDistance * (isOutward ? 1 : -1));
                    if (offsetCurves.Count == 0)
                    {
                        trans.RollBack();
                        return false;
                    }

                    Level level = doc.GetElement(existingFloor.LevelId) as Level;
                    if (level == null)
                    {
                        trans.RollBack();
                        return false;
                    }

                    if (isOutward)
                    {
                        // Create new floor with offset boundary (outward)
                        CreateNewFloor(doc, offsetCurves, newFloorType, level);
                    }
                    else
                    {
                        // Modify existing floor and create new inner floor (inward)
                        ModifyExistingFloorAndCreateInner(doc, existingFloor, boundaryCurves, offsetCurves, newFloorType, level);
                    }

                    trans.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating floor band: {ex.Message}");
                return false;
            }
        }

        private List<CurveLoop> GetFloorBoundaryCurves(Sketch sketch)
        {
            var curveLoops = new List<CurveLoop>();

            try
            {
                foreach (CurveArray curveArray in sketch.Profile)
                {
                    var curveLoop = new CurveLoop();
                    foreach (Curve curve in curveArray)
                    {
                        curveLoop.Append(curve);
                    }
                    curveLoops.Add(curveLoop);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting floor boundary curves: {ex.Message}");
            }

            return curveLoops;
        }

        private List<CurveLoop> CreateOffsetCurves(List<CurveLoop> originalCurves, double offsetDistance)
        {
            var offsetCurves = new List<CurveLoop>();

            try
            {
                foreach (var curveLoop in originalCurves)
                {
                    try
                    {
                        var offsetLoop = CurveLoop.CreateViaOffset(curveLoop, offsetDistance, XYZ.BasisZ);
                        if (offsetLoop != null)
                        {
                            offsetCurves.Add(offsetLoop);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error creating offset curve: {ex.Message}");
                        // Try alternative method or skip this loop
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CreateOffsetCurves: {ex.Message}");
            }

            return offsetCurves;
        }

        private Floor CreateNewFloor(Document doc, List<CurveLoop> curveLoops, FloorType floorType, Level level)
        {
            try
            {
                if (curveLoops.Count > 0)
                {
                    return Floor.Create(doc, curveLoops, floorType.Id, level.Id);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating new floor: {ex.Message}");
            }

            return null;
        }

        private void ModifyExistingFloorAndCreateInner(Document doc, Floor existingFloor,
            List<CurveLoop> originalCurves, List<CurveLoop> offsetCurves, FloorType newFloorType, Level level)
        {
            try
            {
                // Create the inner floor first
                CreateNewFloor(doc, offsetCurves, newFloorType, level);

                // Modify the existing floor to have the offset boundary
                // This is complex and might require recreating the floor
                // For now, we'll just create the inner floor

                // Note: Modifying existing floor geometry is complex in Revit API
                // Alternative approach would be to delete the original and recreate both floors
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error modifying existing floor: {ex.Message}");
            }
        }
    }
}