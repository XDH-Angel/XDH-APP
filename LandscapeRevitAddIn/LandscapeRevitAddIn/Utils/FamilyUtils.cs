using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace LandscapeRevitAddIn.Utils
{
    public static class FamilyUtils
    {
        /// <summary>
        /// Get all family symbols that could be used for planting
        /// </summary>
        public static List<FamilySymbol> GetPlantingFamilies(Document doc)
        {
            var familySymbols = new List<FamilySymbol>();

            try
            {
                // Get all family symbols in the project
                var allFamilySymbols = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .Cast<FamilySymbol>()
                    .Where(fs => fs.Family.FamilyCategory != null)
                    .ToList();

                // Filter for categories that might be plants/trees
                var validCategories = new List<BuiltInCategory>
                {
                    BuiltInCategory.OST_Planting,
                    BuiltInCategory.OST_GenericModel,
                    BuiltInCategory.OST_Furniture,
                    BuiltInCategory.OST_SpecialityEquipment
                };

                foreach (var symbol in allFamilySymbols)
                {
                    var categoryId = symbol.Family.FamilyCategory.Id;
                    var builtInCategory = (BuiltInCategory)categoryId.Value;

                    if (validCategories.Contains(builtInCategory))
                    {
                        familySymbols.Add(symbol);
                    }
                }

                return familySymbols.OrderBy(fs => fs.Family.Name).ThenBy(fs => fs.Name).ToList();
            }
            catch (Exception)
            {
                return new List<FamilySymbol>();
            }
        }

        /// <summary>
        /// Place a family instance at a specific point
        /// </summary>
        public static FamilyInstance PlaceFamilyAtPoint(Document doc, FamilySymbol symbol, XYZ point, Level level)
        {
            try
            {
                // Activate the symbol if it's not already active
                if (!symbol.IsActive)
                {
                    symbol.Activate();
                }

                // Create the family instance
                var familyInstance = doc.Create.NewFamilyInstance(
                    point,
                    symbol,
                    level,
                    Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                return familyInstance;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error placing family: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Get the default level (usually Level 1)
        /// </summary>
        public static Level GetDefaultLevel(Document doc)
        {
            try
            {
                var levels = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .OrderBy(l => l.Elevation)
                    .ToList();

                // Return the first level (lowest elevation)
                return levels.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get a user-friendly display name for a family symbol
        /// </summary>
        public static string GetFamilyDisplayName(FamilySymbol symbol)
        {
            try
            {
                if (symbol.Family.Name == symbol.Name)
                {
                    return symbol.Family.Name;
                }
                else
                {
                    return $"{symbol.Family.Name} : {symbol.Name}";
                }
            }
            catch
            {
                return "Unknown Family";
            }
        }
    }
}