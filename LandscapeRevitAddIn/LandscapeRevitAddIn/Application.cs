using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LandscapeRevitAddIn
{
    public class Application : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Create the main tab
                application.CreateRibbonTab("XD Landscape");
                string assemblyPath = Assembly.GetExecutingAssembly().Location;

                // Create all the panels for the ribbon
                CreatePanel01(application, assemblyPath);
                CreatePanel02(application, assemblyPath);
                CreatePanel03(application, assemblyPath);
                CreatePanel05(application, assemblyPath);
                CreatePanel06(application, assemblyPath); // Added call to create Panel06
                CreatePanel07(application, assemblyPath);
                CreatePanel08(application, assemblyPath);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Startup Error", $"Failed to start the application: {ex.Message}");
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private void CreatePanel01(UIControlledApplication application, string assemblyPath)
        {
            try
            {
                RibbonPanel panel01 = application.CreateRibbonPanel("XD Landscape", "By Linked CAD");

                // Trees button
                PushButtonData treesButtonData = new PushButtonData(
                    "TreesButton", "Trees", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel01.TreesCommand");
                PushButton treesButton = panel01.AddItem(treesButtonData) as PushButton;
                treesButton.ToolTip = "Place trees from CAD lines";
                treesButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel01.Trees_32.png");
                treesButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel01.Trees_16.png");

                // Floor Layout button
                PushButtonData floorButtonData = new PushButtonData(
                    "FloorLayoutButton", "Floor Layout", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel01.FloorLayoutCommand");
                PushButton floorButton = panel01.AddItem(floorButtonData) as PushButton;
                floorButton.ToolTip = "Create floors from CAD boundaries";
                floorButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel01.FloorLayout_32.png");
                floorButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel01.FloorLayout_16.png");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Panel01 Error", $"Failed to create Panel01: {ex.Message}");
            }
        }

        private void CreatePanel02(UIControlledApplication application, string assemblyPath)
        {
            try
            {
                RibbonPanel panel02 = application.CreateRibbonPanel("XD Landscape", "Level Tools");

                // Elements Level Command
                PushButtonData elementsLevelData = new PushButtonData(
                    "ElementsLevelCommand", "Elements\nLevel", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel02.ElementsLevelCommand");
                PushButton elementsLevelButton = panel02.AddItem(elementsLevelData) as PushButton;
                elementsLevelButton.ToolTip = "Adjust level settings for landscape elements";
                elementsLevelButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel02.ElementsLevel_32.png");
                elementsLevelButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel02.ElementsLevel_16.png");

                // Floors Level Command
                PushButtonData floorsLevelData = new PushButtonData(
                    "FloorsLevelCommand", "Floors\nLevel", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel02.FloorsLevelCommand");
                PushButton floorsLevelButton = panel02.AddItem(floorsLevelData) as PushButton;
                floorsLevelButton.ToolTip = "Adjust floor level settings";
                floorsLevelButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel02.FloorsLevel_32.png");
                floorsLevelButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel02.FloorsLevel_16.png");

                // Multiple Lines Level Command
                PushButtonData multipleLinesData = new PushButtonData(
                    "MultipleLinesLevelCommand", "Multiple Lines\nLevel", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel02.MultipleLinesLevelCommand");
                PushButton multipleLinesButton = panel02.AddItem(multipleLinesData) as PushButton;
                multipleLinesButton.ToolTip = "Adjust level for multiple line elements";
                multipleLinesButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel02.MultipleLinesLevels_32.png");
                multipleLinesButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel02.MultipleLinesLevels_16.png");

                // Slope by Lines Command
                PushButtonData slopeLinesData = new PushButtonData(
                    "SlopeByLinesCommand", "Slope by\nLines", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel02.SlopeByLinesCommand");
                PushButton slopeLinesButton = panel02.AddItem(slopeLinesData) as PushButton;
                slopeLinesButton.ToolTip = "Create slopes based on selected lines";
                slopeLinesButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel02.SlopeByLines_32.png");
                slopeLinesButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel02.SlopeByLines_16.png");

                // Walls Level Command
                PushButtonData wallsLevelData = new PushButtonData(
                    "WallsLevelCommand", "Walls\nLevel", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel02.WallsLevelCommand");
                PushButton wallsLevelButton = panel02.AddItem(wallsLevelData) as PushButton;
                wallsLevelButton.ToolTip = "Adjust wall level and height settings";
                wallsLevelButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel02.WallsLevel_32.png");
                wallsLevelButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel02.WallsLevel_16.png");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Panel02 Error", $"Failed to create Panel02: {ex.Message}");
            }
        }

        private void CreatePanel03(UIControlledApplication application, string assemblyPath)
        {
            try
            {
                RibbonPanel panel03 = application.CreateRibbonPanel("XD Landscape", "Craft");

                // Floor Band button
                PushButtonData floorBandData = new PushButtonData(
                    "FloorBandCommand", "Floor\nBand", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel03.FloorBandCommand");
                PushButton floorBandButton = panel03.AddItem(floorBandData) as PushButton;
                floorBandButton.ToolTip = "Create floor band with offset from existing floor";
                floorBandButton.LongDescription = "Create a new floor based on existing floor perimeter with offset. Outward offset creates new floor, inward offset modifies existing floor to avoid overlap.";
                floorBandButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel03.FloorBand_32.png");
                floorBandButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel03.FloorBand_16.png");

                // GeoFam button
                PushButtonData geoFamData = new PushButtonData(
                    "GeoFamCommand", "GeoFam", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel03.GeoFamCommand");
                PushButton geoFamButton = panel03.AddItem(geoFamData) as PushButton;
                geoFamButton.ToolTip = "Create family from existing geometry";
                geoFamButton.LongDescription = "Convert selected geometry from loadable families or in-place families into a new loadable family that can be reused in the project.";
                geoFamButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel03.GeoFam_32.png");
                geoFamButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel03.GeoFam_16.png");

                // VisRef button
                PushButtonData visRefData = new PushButtonData(
                    "VisRefCommand", "VisRef", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel03.VisRefCommand");
                PushButton visRefButton = panel03.AddItem(visRefData) as PushButton;
                visRefButton.ToolTip = "Create visual reference geometry";
                visRefButton.LongDescription = "Create geometry to visualize target references by taking selected edges and generating surfaces for spatial visualization.";
                visRefButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel03.VisRef_32.png");
                visRefButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel03.VisRef_16.png");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Panel03 Error", $"Failed to create Panel03: {ex.Message}");
            }
        }

        private void CreatePanel05(UIControlledApplication application, string assemblyPath)
        {
            try
            {
                RibbonPanel panel05 = application.CreateRibbonPanel("XD Landscape", "Align Face");

                // CAD Points button
                PushButtonData cadPointsData = new PushButtonData(
                    "CadPointsCommand", "CAD\nPoints", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel05.CadPointsCommand");
                PushButton cadPointsButton = panel05.AddItem(cadPointsData) as PushButton;
                cadPointsButton.ToolTip = "Surface by Points taken from CAD reference";
                cadPointsButton.LongDescription = "Create or adjust floor surface using elevation points from a selected CAD layer. Points from the CAD layer will be used to generate a surface that the floor will conform to.";
                cadPointsButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel05.CadPoints_32.png");
                cadPointsButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel05.CadPoints_16.png");

                // Slope Value button
                PushButtonData slopeValueData = new PushButtonData(
                    "SlopeValueCommand", "Slope\nValue", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel05.SlopeValueCommand");
                PushButton slopeValueButton = panel05.AddItem(slopeValueData) as PushButton;
                slopeValueButton.ToolTip = "Apply slope to floor with direction and value";
                slopeValueButton.LongDescription = "Select floor and define slope direction and value (percentage or degrees). The floor will be modified to have the specified slope in the chosen direction.";
                slopeValueButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel05.SlopeValue_32.png");
                slopeValueButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel05.SlopeValue_16.png");

                // Surface button
                PushButtonData surfaceData = new PushButtonData(
                    "SurfaceCommand", "Surface", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel05.SurfaceCommand");
                PushButton surfaceButton = panel05.AddItem(surfaceData) as PushButton;
                surfaceButton.ToolTip = "Use existing face to adjust floor surface";
                surfaceButton.LongDescription = "Select a floor and a reference surface face. The reference surface will be projected and expanded to adjust the floor surface accordingly.";
                surfaceButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel05.Surface_32.png");
                surfaceButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel05.Surface_16.png");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Panel05 Error", $"Failed to create Panel05: {ex.Message}");
            }
        }

        private void CreatePanel06(UIControlledApplication application, string assemblyPath)
        {
            try
            {
                RibbonPanel panel06 = application.CreateRibbonPanel("XD Landscape", "Align Floor Edge");

                // One Edge button
                PushButtonData oneEdgeData = new PushButtonData(
                    "OneEdgeCommand", "One\nEdge", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel06.OneEdgeCommand");
                PushButton oneEdgeButton = panel06.AddItem(oneEdgeData) as PushButton;
                oneEdgeButton.ToolTip = "Align floor edges to reference edge";
                oneEdgeButton.LongDescription = "Select one or more floor edges to be aligned, then select a reference edge. The selected floor edges will be modified to align with the reference edge.";
                oneEdgeButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel06.OneEdge_32.png");
                oneEdgeButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel06.OneEdge_16.png");

                // Two Edge button
                PushButtonData twoEdgeData = new PushButtonData(
                    "TwoEdgeCommand", "Two\nEdge", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel06.TwoEdgeCommand");
                PushButton twoEdgeButton = panel06.AddItem(twoEdgeData) as PushButton;
                twoEdgeButton.ToolTip = "Align floor to virtual surface from two edges";
                twoEdgeButton.LongDescription = "Select a floor to modify, then select two reference edges. A virtual surface will be created from the two edges and the floor will be aligned to this surface.";
                twoEdgeButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel06.TwoEdge_32.png");
                twoEdgeButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel06.TwoEdge_16.png");

                // By Surface button
                PushButtonData bySurfaceData = new PushButtonData(
                    "BySurfaceCommand", "By\nSurface", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel06.BySurfaceCommand");
                PushButton bySurfaceButton = panel06.AddItem(bySurfaceData) as PushButton;
                bySurfaceButton.ToolTip = "Align floor edges to reference surface";
                bySurfaceButton.LongDescription = "Select floor edges to be modified, then select a reference surface. The selected floor edges will be projected onto the reference surface plane.";
                bySurfaceButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel06.BySurface_32.png");
                bySurfaceButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel06.BySurface_16.png");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Panel06 Error", $"Failed to create Panel06: {ex.Message}");
            }
        }

        private void CreatePanel07(UIControlledApplication application, string assemblyPath)
        {
            try
            {
                RibbonPanel panel07 = application.CreateRibbonPanel("XD Landscape", "Floor Points");

                // One Edge button (for points)
                PushButtonData oneEdgePointsData = new PushButtonData(
                    "OneEdgePointsCommand", "One\nEdge", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel07.OneEdgeCommand");
                PushButton oneEdgePointsButton = panel07.AddItem(oneEdgePointsData) as PushButton;
                oneEdgePointsButton.ToolTip = "Align floor points to reference edge";
                oneEdgePointsButton.LongDescription = "Select floor points to be aligned, then select a reference edge. The selected floor points will be projected onto the reference edge plane.";
                oneEdgePointsButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel07.OneEdge_32.png");
                oneEdgePointsButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel07.OneEdge_16.png");

                // Two Edge button (for points)
                PushButtonData twoEdgePointsData = new PushButtonData(
                    "TwoEdgePointsCommand", "Two\nEdge", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel07.TwoEdgeCommand");
                PushButton twoEdgePointsButton = panel07.AddItem(twoEdgePointsData) as PushButton;
                twoEdgePointsButton.ToolTip = "Align floor points to virtual surface from two edges";
                twoEdgePointsButton.LongDescription = "Select floor points to be aligned, then select two reference edges. A virtual surface will be created from the two edges and the floor points will be aligned to this surface.";
                twoEdgePointsButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel07.TwoEdge_32.png");
                twoEdgePointsButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel07.TwoEdge_16.png");

                // By Surface button (for points)
                PushButtonData bySurfacePointsData = new PushButtonData(
                    "BySurfacePointsCommand", "By\nSurface", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel07.BySurfaceCommand");
                PushButton bySurfacePointsButton = panel07.AddItem(bySurfacePointsData) as PushButton;
                bySurfacePointsButton.ToolTip = "Align floor points to reference surface";
                bySurfacePointsButton.LongDescription = "Select floor points to be aligned, then select a reference surface. The selected floor points will be projected onto the reference surface plane.";
                bySurfacePointsButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel07.BySurface_32.png");
                bySurfacePointsButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel07.BySurface_16.png");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Panel07 Error", $"Failed to create Panel07: {ex.Message}");
            }
        }

        private void CreatePanel08(UIControlledApplication application, string assemblyPath)
        {
            try
            {
                RibbonPanel panel08 = application.CreateRibbonPanel("XD Landscape", "Advanced Tools");

                // Annotation Command
                PushButtonData annotationData = new PushButtonData(
                    "AnnotationCommand", "Annotation", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel08.AnnotationCommand");
                PushButton annotationButton = panel08.AddItem(annotationData) as PushButton;
                annotationButton.ToolTip = "Create annotations and labels";
                annotationButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel08.Annotation_32.png");
                annotationButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel08.Annotation_16.png");

                // Create Sheets By Scope Box Command
                PushButtonData createSheetsData = new PushButtonData(
                    "CreateSheetsByScopeBoxCommand", "Create Sheets\nBy Scope Box", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel08.CreateSheetsByScopeBoxCommand");
                PushButton createSheetsButton = panel08.AddItem(createSheetsData) as PushButton;
                createSheetsButton.ToolTip = "Create sheets based on scope box boundaries";
                createSheetsButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel08.CreateSheetsByScopeBox_32.png");
                createSheetsButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel08.CreateSheetsByScopeBox_16.png");

                // Filled Region By Face Command
                PushButtonData filledRegionData = new PushButtonData(
                    "FilledRegionByFaceCommand", "Filled Region\nBy Face", assemblyPath,
                    "LandscapeRevitAddIn.Commands.Panel08.FilledRegionByFaceCommand");
                PushButton filledRegionButton = panel08.AddItem(filledRegionData) as PushButton;
                filledRegionButton.ToolTip = "Create filled regions from face boundaries";
                filledRegionButton.LargeImage = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel08.FilledRegionByFace_32.png");
                filledRegionButton.Image = LoadEmbeddedImage("LandscapeRevitAddIn.Resources.Icons.Panel08.FilledRegionByFace_16.png");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Panel08 Error", $"Failed to create Panel08: {ex.Message}");
            }
        }

        private ImageSource LoadEmbeddedImage(string resourceName)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                Stream stream = assembly.GetManifestResourceStream(resourceName);

                if (stream != null)
                {
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = stream;
                    image.EndInit();
                    return image;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading image {resourceName}: {ex.Message}");
            }
            return null;
        }
    }
}
