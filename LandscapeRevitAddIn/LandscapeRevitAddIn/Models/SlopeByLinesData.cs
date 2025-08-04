// Models/SlopeByLinesData.cs
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace LandscapeRevitAddIn.Models
{
    public class SlopeByLinesData
    {
        public List<LineSlopeData> LineSlopes { get; set; }

        public SlopeByLinesData()
        {
            LineSlopes = new List<LineSlopeData>();
        }
    }

    public class LineSlopeData
    {
        public Line ReferenceLine { get; set; }
        public double SlopePercentage { get; set; }
        public Level TargetLevel { get; set; }
    }
}