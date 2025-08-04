using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using LandscapeRevitAddIn.Utils;

namespace LandscapeRevitAddIn.UI.Windows.Panel08
{
    public partial class FilledRegionByFaceWindow : Window
    {
        private readonly UIDocument _uiDocument;
        private readonly Document _document;
        private IList<Reference> _selectedFaces;
        private List<FilledRegionTypeViewModel> _filledRegionTypes;

        public FilledRegionByFaceWindow(UIDocument uiDocument)
        {
            InitializeComponent();
            _uiDocument = uiDocument;
            _document = uiDocument.Document;
            _selectedFaces = new List<Reference>();
            
            LoadFilledRegionTypes();
            UpdateUI();
        }

        private void LoadFilledRegionTypes()
        {
            _filledRegionTypes = new List<FilledRegionTypeViewModel>();
            
            var filledRegionTypes = new FilteredElementCollector(_document)
                .OfClass(typeof(FilledRegionType))
                .Cast<FilledRegionType>()
                .OrderBy(t => t.Name)
                .ToList();

            foreach (var type in filledRegionTypes)
            {
                _filledRegionTypes.Add(new FilledRegionTypeViewModel
                {
                    FilledRegionType = type,
                    Name = type.Name,
                    PreviewBrush = GetPreviewBrush(type)
                });
            }

            FilledRegionTypeListBox.ItemsSource = _filledRegionTypes;
            
            if (_filledRegionTypes.Count > 0)
            {
                FilledRegionTypeListBox.SelectedIndex = 0;
            }
        }

        private Brush GetPreviewBrush(FilledRegionType type)
        {
            try
            {
                // Try to get the fill pattern color - simplified for now
                return new SolidColorBrush(Colors.Gray);
            }
            catch
            {
                return new SolidColorBrush(Colors.LightGray);
            }
        }

        private void SelectFacesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Hide the window temporarily
                this.Hide();

                // Select faces
                _selectedFaces = _uiDocument.Selection.PickObjects(
                    ObjectType.Face,
                    "Select faces to create filled regions from");

                // Show the window again
                this.Show();
                this.Activate();

                UpdateUI();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // User cancelled selection - show window again
                this.Show();
                this.Activate();
            }
            catch (Exception ex)
            {
                this.Show();
                this.Activate();
                RevitUtils.ShowMessage("Error", $"Failed to select faces: {ex.Message}");
            }
        }

        private void UpdateUI()
        {
            // Update selected faces text
            if (_selectedFaces?.Count > 0)
            {
                SelectedFacesText.Text = $"{_selectedFaces.Count} face(s) selected";
                SelectedFacesText.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                SelectedFacesText.Text = "No faces selected";
                SelectedFacesText.Foreground = new SolidColorBrush(Colors.Gray);
            }

            // Update summary
            bool canCreate = _selectedFaces?.Count > 0 && FilledRegionTypeListBox.SelectedItem != null;
            CreateFilledRegionsButton.IsEnabled = canCreate;

            if (canCreate)
            {
                var selectedType = (FilledRegionTypeViewModel)FilledRegionTypeListBox.SelectedItem;
                SummaryTextBlock.Text = $"Ready to create {_selectedFaces.Count} filled region(s) using '{selectedType.Name}'";
            }
            else
            {
                SummaryTextBlock.Text = "Select faces and a filled region type to continue";
            }
        }

        private void FilledRegionTypeListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateUI();
        }

        private void CreateFilledRegionsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedFaces == null || _selectedFaces.Count == 0)
                {
                    RevitUtils.ShowMessage("Error", "No faces selected.");
                    return;
                }

                var selectedTypeViewModel = (FilledRegionTypeViewModel)FilledRegionTypeListBox.SelectedItem;
                if (selectedTypeViewModel == null)
                {
                    RevitUtils.ShowMessage("Error", "No filled region type selected.");
                    return;
                }

                // Check if current view supports filled regions
                if (!IsValidViewForFilledRegion(_document.ActiveView))
                {
                    RevitUtils.ShowMessage("Error", "Filled regions can only be created in plan views, sections, elevations, or drafting views.");
                    return;
                }

                using (Transaction trans = new Transaction(_document, "Create Filled Regions by Face"))
                {
                    trans.Start();

                    int successCount = 0;
                    foreach (Reference faceRef in _selectedFaces)
                    {
                        try
                        {
                            if (CreateFilledRegionFromFace(_document, faceRef, selectedTypeViewModel.FilledRegionType))
                                successCount++;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to create filled region from face: {ex.Message}");
                        }
                    }

                    trans.Commit();

                    RevitUtils.ShowMessage("Success", $"Created {successCount} filled regions from {_selectedFaces.Count} selected faces.");
                    
                    if (successCount > 0)
                    {
                        this.DialogResult = true;
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                RevitUtils.ShowMessage("Error", $"Failed to create filled regions: {ex.Message}");
            }
        }

        private bool IsValidViewForFilledRegion(View view)
        {
            return view.ViewType == ViewType.FloorPlan ||
                   view.ViewType == ViewType.CeilingPlan ||
                   view.ViewType == ViewType.Section ||
                   view.ViewType == ViewType.Elevation ||
                   view.ViewType == ViewType.DraftingView;
        }

        private bool CreateFilledRegionFromFace(Document doc, Reference faceRef, FilledRegionType regionType)
        {
            try
            {
                Element element = doc.GetElement(faceRef);
                Face face = null;
                GeometryElement geomElem = element.get_Geometry(new Options());
                foreach (GeometryObject geomObj in geomElem)
                {
                    if (geomObj is Face)
                    {
                        face = geomObj as Face;
                        break;
                    }
                }

                if (face == null) return false;

                // Get face boundary curves
                IList<CurveLoop> curveLoops = face.GetEdgesAsCurveLoops();
                if (curveLoops == null || curveLoops.Count == 0) return false;

                // Filter valid loops (need at least 3 curves)
                List<CurveLoop> validLoops = new List<CurveLoop>();
                foreach (CurveLoop loop in curveLoops)
                {
                    if (loop.Count() > 2)
                    {
                        validLoops.Add(loop); 
                    }
                }

                if (validLoops.Count > 0)
                {
                    FilledRegion.Create(doc, regionType.Id, doc.ActiveView.Id, validLoops);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }

    // ViewModel for FilledRegionType display
    public class FilledRegionTypeViewModel
    {
        public FilledRegionType FilledRegionType { get; set; }
        public string Name { get; set; }
        public Brush PreviewBrush { get; set; }
    }
}