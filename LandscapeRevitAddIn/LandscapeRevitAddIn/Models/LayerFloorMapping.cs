using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace LandscapeRevitAddIn.Models
{
    public class LayerFloorMapping
    {
        public string LayerName { get; set; }
        public FloorType FloorType { get; set; }
        public int BoundaryCount { get; set; }
        public bool IsSelected { get; set; }

        public LayerFloorMapping()
        {
            IsSelected = false;
            BoundaryCount = 0;
        }

        public string DisplayText => $"{LayerName} ({BoundaryCount} boundaries)";
        public string FloorTypeDisplayText => FloorType != null ? FloorType.Name : "No floor type assigned";
    }
}