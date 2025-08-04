// Models/LevelAdjustmentData.cs
using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace LandscapeRevitAddIn.Models
{
    public class LevelAdjustmentData
    {
        public Level TargetLevel { get; set; }
        public double ElevationOffset { get; set; }
        public bool AdjustElevation { get; set; }
    }
}

