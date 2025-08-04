using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using LandscapeRevitAddIn.UI.Windows.Panel03;

namespace LandscapeRevitAddIn.Commands.Panel03
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class GeoFamCommand : IExternalCommand
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

                // Show the GeoFam window
                var geoFamWindow = new GeoFamWindow(doc);
                var dialogResult = geoFamWindow.ShowDialog();

                if (dialogResult == true)
                {
                    var selectedElements = geoFamWindow.SelectedElements;
                    var familyName = geoFamWindow.FamilyName;
                    var familyCategory = geoFamWindow.SelectedCategory;
                    var savePath = geoFamWindow.SavePath;

                    if (selectedElements != null && selectedElements.Any() &&
                        !string.IsNullOrEmpty(familyName) && familyCategory != null)
                    {
                        var result = CreateFamilyFromGeometry(doc, selectedElements, familyName, familyCategory, savePath);

                        if (result)
                        {
                            TaskDialog.Show("Success",
                                $"Family '{familyName}' created successfully from {selectedElements.Count()} element(s)." +
                                (string.IsNullOrEmpty(savePath) ? "" : $"\nSaved to: {savePath}"));
                        }
                        else
                        {
                            TaskDialog.Show("Error", "Failed to create family from geometry. Please check the selected elements and try again.");
                        }
                    }
                    else
                    {
                        TaskDialog.Show("No Selection", "Please select elements and configure family settings.");
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

        private bool CreateFamilyFromGeometry(Document doc, IEnumerable<Element> selectedElements,
            string familyName, Category category, string savePath)
        {
            try
            {
                // Create a new family document
                var familyTemplate = GetFamilyTemplate(doc.Application, category);
                if (string.IsNullOrEmpty(familyTemplate))
                {
                    TaskDialog.Show("Error", "Could not find appropriate family template for the selected category.");
                    return false;
                }

                var familyDoc = doc.Application.NewFamilyDocument(familyTemplate);
                if (familyDoc == null)
                {
                    TaskDialog.Show("Error", "Failed to create new family document.");
                    return false;
                }

                using (Transaction trans = new Transaction(familyDoc, "Create Family Geometry"))
                {
                    trans.Start();

                    var geometryCreated = false;

                    foreach (var element in selectedElements)
                    {
                        if (CreateGeometryInFamily(familyDoc, element))
                        {
                            geometryCreated = true;
                        }
                    }

                    if (!geometryCreated)
                    {
                        trans.RollBack();
                        familyDoc.Close(false);
                        return false;
                    }

                    trans.Commit();
                }

                // Save the family
                var fileName = string.IsNullOrEmpty(savePath) ?
                    Path.Combine(Path.GetTempPath(), $"{familyName}.rfa") :
                    Path.Combine(savePath, $"{familyName}.rfa");

                familyDoc.SaveAs(fileName);

                // Load the family into the project
                using (Transaction trans = new Transaction(doc, "Load Family"))
                {
                    trans.Start();

                    Family family;
                    if (doc.LoadFamily(fileName, out family))
                    {
                        trans.Commit();
                        familyDoc.Close(false);
                        return true;
                    }
                    else
                    {
                        trans.RollBack();
                    }
                }

                familyDoc.Close(false);
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating family from geometry: {ex.Message}");
                return false;
            }
        }

        private string GetFamilyTemplate(Autodesk.Revit.ApplicationServices.Application app, Category category)
        {
            try
            {
                var templatePath = app.FamilyTemplatePath;

                // Map categories to appropriate templates using if-else instead of switch expressions
                string templateName;
                var categoryName = category.Name.ToLower();

                if (categoryName.Contains("planting"))
                    templateName = "Generic Model.rft";
                else if (categoryName.Contains("furniture"))
                    templateName = "Furniture.rft";
                else if (categoryName.Contains("lighting"))
                    templateName = "Lighting Fixture.rft";
                else if (categoryName.Contains("site"))
                    templateName = "Site.rft";
                else
                    templateName = "Generic Model.rft";

                var fullPath = Path.Combine(templatePath, templateName);

                if (File.Exists(fullPath))
                    return fullPath;

                // Fallback to Generic Model if specific template not found
                var genericPath = Path.Combine(templatePath, "Generic Model.rft");
                if (File.Exists(genericPath))
                    return genericPath;

                // Try alternate paths
                var alternatePaths = new[]
                {
                    Path.Combine(templatePath, "Metric Generic Model.rft"),
                    Path.Combine(templatePath, "Generic Model.rft")
                };

                foreach (var path in alternatePaths)
                {
                    if (File.Exists(path))
                        return path;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting family template: {ex.Message}");
                return null;
            }
        }

        private bool CreateGeometryInFamily(Document familyDoc, Element sourceElement)
        {
            try
            {
                var geometryElement = sourceElement.get_Geometry(new Options());
                if (geometryElement == null)
                    return false;

                var solids = ExtractSolids(geometryElement);
                if (!solids.Any())
                    return false;

                // Create free form elements from solids
                foreach (var solid in solids)
                {
                    try
                    {
                        if (solid.Volume > 0)
                        {
                            FreeFormElement.Create(familyDoc, solid);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error creating free form element: {ex.Message}");
                        // Continue with other solids
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating geometry in family: {ex.Message}");
                return false;
            }
        }

        private List<Solid> ExtractSolids(GeometryElement geometryElement)
        {
            var solids = new List<Solid>();

            try
            {
                foreach (GeometryObject geoObj in geometryElement)
                {
                    if (geoObj is Solid solid && solid.Volume > 0)
                    {
                        solids.Add(solid);
                    }
                    else if (geoObj is GeometryInstance geoInstance)
                    {
                        var instanceGeometry = geoInstance.GetInstanceGeometry();
                        if (instanceGeometry != null)
                        {
                            solids.AddRange(ExtractSolids(instanceGeometry));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting solids: {ex.Message}");
            }

            return solids;
        }
    }
}