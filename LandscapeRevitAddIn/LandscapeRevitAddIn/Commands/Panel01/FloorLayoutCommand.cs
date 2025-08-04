using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using LandscapeRevitAddIn.UI.Windows;
using LandscapeRevitAddIn.Utils;
using LandscapeRevitAddIn.Models;
using LandscapeRevitAddIn.UI.Windows.Panel01;

namespace LandscapeRevitAddIn.Commands.Panel01
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class FloorLayoutCommand : IExternalCommand
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

                var floorWindow = new FloorLayoutWindow(doc);
                var dialogResult = floorWindow.ShowDialog();

                if (dialogResult == true)
                {
                    var validMappings = floorWindow.GetValidMappings();
                    var cadFile = floorWindow.SelectedCADFile;

                    if (validMappings.Count > 0 && cadFile != null)
                    {
                        var results = CreateFloorsByMappings(doc, cadFile, validMappings);
                        string resultMessage = CreateResultMessage(results);
                        TaskDialog.Show("Floors Created", resultMessage);
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

        private Dictionary<string, int> CreateFloorsByMappings(Document doc, ImportInstance cadFile, List<LayerFloorMapping> mappings)
        {
            var results = new Dictionary<string, int>();

            try
            {
                Level defaultLevel = FloorUtils.GetDefaultLevel(doc);
                if (defaultLevel == null)
                {
                    TaskDialog.Show("Error", "No levels found in the project.");
                    return results;
                }

                using (Transaction trans = new Transaction(doc, "Create Floors from CAD Layers"))
                {
                    trans.Start();

                    var allLayersWithBoundaries = FloorUtils.GetLayersWithClosedBoundaries(cadFile);

                    foreach (var mapping in mappings)
                    {
                        int createdCount = 0;

                        try
                        {
                            if (allLayersWithBoundaries.ContainsKey(mapping.LayerName))
                            {
                                var layerBoundaries = allLayersWithBoundaries[mapping.LayerName];

                                foreach (var boundary in layerBoundaries)
                                {
                                    try
                                    {
                                        Floor floor = FloorUtils.CreateFloorFromBoundary(
                                            doc, boundary, mapping.FloorType, defaultLevel);

                                        if (floor != null)
                                        {
                                            createdCount++;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Error creating floor: {ex.Message}");
                                    }
                                }
                            }

                            results[mapping.LayerName] = createdCount;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing layer {mapping.LayerName}: {ex.Message}");
                            results[mapping.LayerName] = createdCount;
                        }
                    }

                    trans.Commit();
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Error creating floors: {ex.Message}");
            }

            return results;
        }

        private string CreateResultMessage(Dictionary<string, int> results)
        {
            if (results.Count == 0)
            {
                return "No floors were created.";
            }

            var totalCreated = results.Values.Sum();
            var messageLines = new List<string>();

            messageLines.Add($"Successfully created {totalCreated} floors total:");
            messageLines.Add("");

            foreach (var result in results)
            {
                messageLines.Add($"• {result.Key}: {result.Value} floors");
            }

            return string.Join("\n", messageLines);
        }
    }
}