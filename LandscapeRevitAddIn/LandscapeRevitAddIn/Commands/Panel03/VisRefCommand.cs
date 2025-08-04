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
    public class VisRefCommand : IExternalCommand
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

                // Show the VisRef window
                var visRefWindow = new VisRefWindow(doc);
                var dialogResult = visRefWindow.ShowDialog();

                if (dialogResult == true)
                {
                    var selectedEdges = visRefWindow.SelectedEdges;
                    var surfaceType = visRefWindow.SurfaceType;
                    var materialId = visRefWindow.SelectedMaterialId;

                    if (selectedEdges != null && selectedEdges.Any())
                    {
                        var result = CreateVisualReference(doc, selectedEdges, surfaceType, materialId);

                        if (result)
                        {
                            TaskDialog.Show("Success",
                                $"Visual reference geometry created successfully from {selectedEdges.Count()} edge(s).");
                        }
                        else
                        {
                            TaskDialog.Show("Error", "Failed to create visual reference geometry. Please check the selected edges and try again.");
                        }
                    }
                    else
                    {
                        TaskDialog.Show("No Selection", "Please select edges to create visual reference.");
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

        private bool CreateVisualReference(Document doc, IEnumerable<Edge> selectedEdges, string surfaceType, ElementId materialId)
        {
            try
            {
                using (Transaction trans = new Transaction(doc, "Create Visual Reference"))
                {
                    trans.Start();

                    var curves = ExtractCurvesFromEdges(selectedEdges);
                    if (!curves.Any())
                    {
                        trans.RollBack();
                        return false;
                    }

                    DirectShape visualRef = null;

                    switch (surfaceType.ToLower())
                    {
                        case "loft":
                            visualRef = CreateLoftedSurface(doc, curves, materialId);
                            break;
                        case "ruled":
                            visualRef = CreateRuledSurface(doc, curves, materialId);
                            break;
                        case "planar":
                            visualRef = CreatePlanarSurface(doc, curves, materialId);
                            break;
                        default:
                            visualRef = CreateLoftedSurface(doc, curves, materialId);
                            break;
                    }

                    if (visualRef == null)
                    {
                        trans.RollBack();
                        return false;
                    }

                    trans.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating visual reference: {ex.Message}");
                return false;
            }
        }

        private List<Curve> ExtractCurvesFromEdges(IEnumerable<Edge> edges)
        {
            var curves = new List<Curve>();

            try
            {
                foreach (var edge in edges)
                {
                    var curve = edge.AsCurve();
                    if (curve != null)
                    {
                        curves.Add(curve);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting curves from edges: {ex.Message}");
            }

            return curves;
        }

        private DirectShape CreateLoftedSurface(Document doc, List<Curve> curves, ElementId materialId)
        {
            try
            {
                if (curves.Count < 2) return null;

                // Create curve loops from the curves
                var curveLoops = new List<CurveLoop>();

                // Try to group curves that form closed loops
                var remainingCurves = new List<Curve>(curves);

                while (remainingCurves.Any())
                {
                    var curveLoop = new CurveLoop();
                    var currentCurve = remainingCurves.First();
                    curveLoop.Append(currentCurve);
                    remainingCurves.Remove(currentCurve);

                    // Try to find connecting curves
                    var endPoint = currentCurve.GetEndPoint(1);
                    var foundConnection = true;

                    while (foundConnection && remainingCurves.Any())
                    {
                        foundConnection = false;
                        for (int i = 0; i < remainingCurves.Count; i++)
                        {
                            var testCurve = remainingCurves[i];
                            if (endPoint.IsAlmostEqualTo(testCurve.GetEndPoint(0), 0.01))
                            {
                                curveLoop.Append(testCurve);
                                endPoint = testCurve.GetEndPoint(1);
                                remainingCurves.RemoveAt(i);
                                foundConnection = true;
                                break;
                            }
                            else if (endPoint.IsAlmostEqualTo(testCurve.GetEndPoint(1), 0.01))
                            {
                                curveLoop.Append(testCurve.CreateReversed());
                                endPoint = testCurve.GetEndPoint(0);
                                remainingCurves.RemoveAt(i);
                                foundConnection = true;
                                break;
                            }
                        }
                    }

                    if (curveLoop.NumberOfCurves() > 0)
                    {
                        curveLoops.Add(curveLoop);
                    }
                }

                if (curveLoops.Count >= 2)
                {
                    // Create loft between curve loops
                    var loftOptions = new SolidOptions(ElementId.InvalidElementId, materialId);

                    try
                    {
                        var loftedGeom = GeometryCreationUtilities.CreateLoftGeometry(curveLoops, loftOptions);

                        if (loftedGeom != null)
                        {
                            var geometryObjects = new List<GeometryObject> { loftedGeom };
                            var directShape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                            directShape.SetShape(geometryObjects);
                            return directShape;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error creating loft geometry: {ex.Message}");
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating lofted surface: {ex.Message}");
                return null;
            }
        }

        private DirectShape CreateRuledSurface(Document doc, List<Curve> curves, ElementId materialId)
        {
            try
            {
                if (curves.Count < 2) return null;

                // For ruled surfaces, we'll create a simple loft between consecutive curves
                var curveLoops = new List<CurveLoop>();

                for (int i = 0; i < curves.Count; i++)
                {
                    try
                    {
                        var curveLoop = new CurveLoop();
                        curveLoop.Append(curves[i]);
                        curveLoops.Add(curveLoop);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error creating curve loop {i}: {ex.Message}");
                        continue;
                    }
                }

                if (curveLoops.Count >= 2)
                {
                    try
                    {
                        // Create loft between curve loops (this creates a solid)
                        var loftOptions = new SolidOptions(ElementId.InvalidElementId, materialId);
                        var loftedGeom = GeometryCreationUtilities.CreateLoftGeometry(curveLoops, loftOptions);

                        if (loftedGeom != null)
                        {
                            var geometryObjects = new List<GeometryObject> { loftedGeom };
                            var directShape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                            directShape.SetShape(geometryObjects);
                            return directShape;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error creating loft from curves: {ex.Message}");
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating ruled surface: {ex.Message}");
                return null;
            }
        }

        private DirectShape CreatePlanarSurface(Document doc, List<Curve> curves, ElementId materialId)
        {
            try
            {
                if (!curves.Any()) return null;

                // Try to create a planar surface from the curves
                var curveLoop = new CurveLoop();

                // Sort curves to form a continuous loop
                var sortedCurves = SortCurvesToFormLoop(curves);

                foreach (var curve in sortedCurves)
                {
                    curveLoop.Append(curve);
                }

                if (curveLoop.IsOpen())
                {
                    // Try to close the loop by connecting the endpoints
                    var startPoint = curveLoop.First().GetEndPoint(0);
                    var endPoint = curveLoop.Last().GetEndPoint(1);

                    if (!startPoint.IsAlmostEqualTo(endPoint, 0.01))
                    {
                        var closingLine = Line.CreateBound(endPoint, startPoint);
                        curveLoop.Append(closingLine);
                    }
                }

                var curveLoops = new List<CurveLoop> { curveLoop };
                var solidOptions = new SolidOptions(ElementId.InvalidElementId, materialId);

                // Create planar surface
                var planarGeom = GeometryCreationUtilities.CreateExtrusionGeometry(curveLoops, XYZ.BasisZ, 0.1);

                if (planarGeom != null)
                {
                    var directShape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                    directShape.SetShape(new GeometryObject[] { planarGeom });
                    return directShape;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating planar surface: {ex.Message}");
                return null;
            }
        }

        private List<Curve> SortCurvesToFormLoop(List<Curve> curves)
        {
            if (!curves.Any()) return curves;

            var sortedCurves = new List<Curve>();
            var remainingCurves = new List<Curve>(curves);

            // Start with the first curve
            var currentCurve = remainingCurves.First();
            sortedCurves.Add(currentCurve);
            remainingCurves.Remove(currentCurve);

            var currentEndPoint = currentCurve.GetEndPoint(1);

            // Try to connect subsequent curves
            while (remainingCurves.Any())
            {
                Curve nextCurve = null;
                bool reverse = false;

                foreach (var curve in remainingCurves)
                {
                    if (currentEndPoint.IsAlmostEqualTo(curve.GetEndPoint(0), 0.01))
                    {
                        nextCurve = curve;
                        reverse = false;
                        break;
                    }
                    else if (currentEndPoint.IsAlmostEqualTo(curve.GetEndPoint(1), 0.01))
                    {
                        nextCurve = curve;
                        reverse = true;
                        break;
                    }
                }

                if (nextCurve != null)
                {
                    if (reverse)
                    {
                        nextCurve = nextCurve.CreateReversed();
                    }

                    sortedCurves.Add(nextCurve);
                    remainingCurves.Remove(remainingCurves.First(c => c.Reference?.ElementId == nextCurve.Reference?.ElementId));
                    currentEndPoint = nextCurve.GetEndPoint(1);
                }
                else
                {
                    // If no connecting curve found, add the remaining curves as is
                    sortedCurves.AddRange(remainingCurves);
                    break;
                }
            }

            return sortedCurves;
        }
    }
}