// Models/MultipleLinesLevelsData.cs
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace LandscapeRevitAddIn.Models
{
    public class MultipleLinesLevelsData
    {
        public List<FloorLinesLevelsData> FloorAdjustments { get; set; }

        public MultipleLinesLevelsData()
        {
            FloorAdjustments = new List<FloorLinesLevelsData>();
        }
    }

    public class FloorLinesLevelsData
    {
        public Floor Floor { get; set; }
        public List<Line> ReferenceLines { get; set; }
        public List<Level> ReferenceLevels { get; set; }

        public FloorLinesLevelsData()
        {
            ReferenceLines = new List<Line>();
            ReferenceLevels = new List<Level>();
        }
    }
}