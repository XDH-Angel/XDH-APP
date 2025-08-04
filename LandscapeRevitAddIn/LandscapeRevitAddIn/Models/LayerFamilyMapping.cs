using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace LandscapeRevitAddIn.Models
{
    /// <summary>
    /// Represents a mapping between CAD layer and Revit family
    /// </summary>
    public class LayerFamilyMapping
    {
        public string LayerName { get; set; }
        public FamilySymbol FamilySymbol { get; set; }
        public int LineCount { get; set; }
        public bool IsSelected { get; set; }

        public LayerFamilyMapping()
        {
            IsSelected = false;
            LineCount = 0;
        }

        public string DisplayText => $"{LayerName} ({LineCount} lines)";

        public string FamilyDisplayText => FamilySymbol != null ?
            GetFamilyDisplayName(FamilySymbol) : "No family assigned";

        private string GetFamilyDisplayName(FamilySymbol symbol)
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

    /// <summary>
    /// Collection of layer-family mappings
    /// </summary>
    public class LayerMappingCollection
    {
        public List<LayerFamilyMapping> Mappings { get; set; }
        public ImportInstance CADFile { get; set; }

        public LayerMappingCollection()
        {
            Mappings = new List<LayerFamilyMapping>();
        }

        public void AddMapping(string layerName, int lineCount)
        {
            Mappings.Add(new LayerFamilyMapping
            {
                LayerName = layerName,
                LineCount = lineCount,
                IsSelected = false,
                FamilySymbol = null
            });
        }

        public List<LayerFamilyMapping> GetSelectedMappings()
        {
            return Mappings.FindAll(m => m.IsSelected && m.FamilySymbol != null);
        }

        public int GetTotalSelectedLines()
        {
            int total = 0;
            foreach (var mapping in GetSelectedMappings())
            {
                total += mapping.LineCount;
            }
            return total;
        }
    }
}