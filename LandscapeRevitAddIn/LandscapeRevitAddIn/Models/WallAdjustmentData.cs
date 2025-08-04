using Autodesk.Revit.DB;

namespace LandscapeRevitAddIn.Models
{
    /// <summary>
    /// Data model for wall adjustment operations
    /// </summary>
    public class WallAdjustmentData
    {
        /// <summary>
        /// Whether to adjust the base level of walls
        /// </summary>
        public bool AdjustBaseLevel { get; set; }

        /// <summary>
        /// Target base level for walls
        /// </summary>
        public Level BaseLevel { get; set; }

        /// <summary>
        /// Base offset in Revit internal units (feet)
        /// </summary>
        public double BaseOffset { get; set; }

        /// <summary>
        /// Whether to adjust the top level of walls
        /// </summary>
        public bool AdjustTopLevel { get; set; }

        /// <summary>
        /// Target top level for walls
        /// </summary>
        public Level TopLevel { get; set; }

        /// <summary>
        /// Top offset in Revit internal units (feet)
        /// </summary>
        public double TopOffset { get; set; }

        /// <summary>
        /// Whether to adjust the unconnected height of walls
        /// </summary>
        public bool AdjustHeight { get; set; }

        /// <summary>
        /// Height adjustment in Revit internal units (feet)
        /// </summary>
        public double HeightAdjustment { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public WallAdjustmentData()
        {
            AdjustBaseLevel = false;
            AdjustTopLevel = false;
            AdjustHeight = false;
            BaseOffset = 0.0;
            TopOffset = 0.0;
            HeightAdjustment = 0.0;
        }

        /// <summary>
        /// Check if any adjustments are enabled
        /// </summary>
        public bool HasAnyAdjustments
        {
            get
            {
                return AdjustBaseLevel || AdjustTopLevel || AdjustHeight;
            }
        }

        /// <summary>
        /// Get a summary of the adjustments to be made
        /// </summary>
        public string GetSummary()
        {
            var summary = new System.Text.StringBuilder();

            if (AdjustBaseLevel)
            {
                summary.AppendLine($"Base Level: {BaseLevel?.Name ?? "Not Set"}");
                if (BaseOffset != 0)
                {
                    summary.AppendLine($"Base Offset: {BaseOffset * 304.8:F2} mm");
                }
            }

            if (AdjustTopLevel)
            {
                summary.AppendLine($"Top Level: {TopLevel?.Name ?? "Not Set"}");
                if (TopOffset != 0)
                {
                    summary.AppendLine($"Top Offset: {TopOffset * 304.8:F2} mm");
                }
            }

            if (AdjustHeight)
            {
                summary.AppendLine($"Height Adjustment: {HeightAdjustment * 304.8:F2} mm");
            }

            return summary.ToString().Trim();
        }
    }
}