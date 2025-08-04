using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using LandscapeRevitAddIn.UI.Windows;
using LandscapeRevitAddIn.Utils;
using LandscapeRevitAddIn.Models;
using FamilyUtils = LandscapeRevitAddIn.Utils.FamilyUtils;
using LandscapeRevitAddIn.UI.Windows.Panel01;

namespace LandscapeRevitAddIn.Commands.Panel01
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class TreesCommand : IExternalCommand
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

                var treesWindow = new TreesWindow(doc);
                var dialogResult = treesWindow.ShowDialog();

                if (dialogResult == true)
                {
                    var validMappings = treesWindow.GetValidMappings();
                    var cadFile = treesWindow.SelectedCADFile;

                    if (validMappings.Count > 0 && cadFile != null)
                    {
                        var results = PlaceTreesByMappings(doc, cadFile, validMappings);
                        string resultMessage = CreateResultMessage(results);
                        TaskDialog.Show("Trees Placed", resultMessage);
                    }
                    else
                    {
                        TaskDialog.Show("No Selection", "Please configure layer mappings.");
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

        private Dictionary<string, int> PlaceTreesByMappings(Document doc, ImportInstance cadFile, List<LayerFamilyMapping> mappings)
        {
            var results = new Dictionary<string, int>();

            try
            {
                Level defaultLevel = FamilyUtils.GetDefaultLevel(doc);
                if (defaultLevel == null)
                {
                    TaskDialog.Show("Error", "No levels found in the project.");
                    return results;
                }

                using (Transaction trans = new Transaction(doc, "Place Trees from CAD Layers"))
                {
                    trans.Start();

                    var allLayersWithLines = CADUtils.GetLayersWithLines(cadFile);

                    foreach (var mapping in mappings)
                    {
                        int placedCount = 0;

                        try
                        {
                            if (!mapping.FamilySymbol.IsActive)
                            {
                                mapping.FamilySymbol.Activate();
                            }

                            if (allLayersWithLines.ContainsKey(mapping.LayerName))
                            {
                                var layerLines = allLayersWithLines[mapping.LayerName];

                                foreach (Line line in layerLines)
                                {
                                    try
                                    {
                                        XYZ midpoint = CADUtils.GetLineMidpoint(line);
                                        FamilyInstance instance = FamilyUtils.PlaceFamilyAtPoint(
                                            doc, mapping.FamilySymbol, midpoint, defaultLevel);

                                        if (instance != null)
                                        {
                                            placedCount++;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Error placing tree: {ex.Message}");
                                    }
                                }
                            }

                            results[mapping.LayerName] = placedCount;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing layer {mapping.LayerName}: {ex.Message}");
                            results[mapping.LayerName] = placedCount;
                        }
                    }

                    trans.Commit();
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Error placing trees: {ex.Message}");
            }

            return results;
        }

        private string CreateResultMessage(Dictionary<string, int> results)
        {
            if (results.Count == 0)
            {
                return "No trees were placed.";
            }

            var totalPlaced = results.Values.Sum();
            var messageLines = new List<string>();

            messageLines.Add($"Successfully placed {totalPlaced} trees total:");
            messageLines.Add("");

            foreach (var result in results)
            {
                messageLines.Add($"• {result.Key}: {result.Value} trees");
            }

            return string.Join("\n", messageLines);
        }
    }
}