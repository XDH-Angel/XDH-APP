using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LandscapeRevitAddIn.Utils
{
    public static class P5FloorUtils
    {
        public static bool AdjustFloorByPoints(Floor floor, List<XYZ> points)
        {
            if (floor == null || points == null || !points.Any()) return false;
            try
            {
                // FIX: Use the modern GetSlabShapeEditor() method
                var slabShapeEditor = floor.GetSlabShapeEditor();
                if (slabShapeEditor == null) return false;

                if (!slabShapeEditor.IsEnabled)
                {
                    slabShapeEditor.Enable();
                    if (!slabShapeEditor.IsEnabled) return false;
                }
                
                slabShapeEditor.ResetSlabShape();
                foreach (var point in points)
                {
                    slabShapeEditor.DrawPoint(point);
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adjusting floor by points: {ex.Message}");
                return false;
            }
        }

        public static bool AdjustFloorByReferenceSurface(Floor floor, Face referenceFace)
        {
            if (floor == null || referenceFace == null) return false;
            try
            {
                // FIX: Use the modern GetSlabShapeEditor() method
                var slabShapeEditor = floor.GetSlabShapeEditor();
                if (slabShapeEditor == null) return false;

                if (!slabShapeEditor.IsEnabled)
                {
                    slabShapeEditor.Enable();
                    if (!slabShapeEditor.IsEnabled) return false;
                }

                var vertices = slabShapeEditor.SlabShapeVertices;
                foreach (SlabShapeVertex vertex in vertices)
                {
                    var currentPos = vertex.Position;
                    var projectionResult = referenceFace.Project(currentPos);
                    if (projectionResult != null)
                    {
                        double offset = projectionResult.XYZPoint.Z - currentPos.Z;
                        slabShapeEditor.ModifySubElement(vertex, offset);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adjusting floor by reference surface: {ex.Message}");
                return false;
            }
        }
    }
}
