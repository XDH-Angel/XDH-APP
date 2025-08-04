using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using LandscapeRevitAddIn.Models;

namespace LandscapeRevitAddIn.UI.Windows.Panel02
{
    public partial class WallAdjustmentWindow : Window
    {
        private Document _doc;
        private List<Wall> _walls;
        private List<Level> _levels;

        public WallAdjustmentWindow(Document doc, List<Wall> walls)
        {
            InitializeComponent();
            _doc = doc;
            _walls = walls;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // Load all levels in the project
                _levels = new FilteredElementCollector(_doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .OrderBy(l => l.Elevation)
                    .ToList();

                // Populate level combo boxes
                PopulateLevelComboBoxes();

                // Set initial values
                SetInitialValues();

                // Update wall count display
                UpdateWallCountDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateLevelComboBoxes()
        {
            if (BaseLevelComboBox != null)
            {
                BaseLevelComboBox.ItemsSource = _levels;
                BaseLevelComboBox.DisplayMemberPath = "Name";
            }

            if (TopLevelComboBox != null)
            {
                TopLevelComboBox.ItemsSource = _levels;
                TopLevelComboBox.DisplayMemberPath = "Name";
            }
        }

        private void SetInitialValues()
        {
            if (_walls.Count > 0)
            {
                var firstWall = _walls.First();

                // Set base level
                try
                {
                    var baseLevelParam = firstWall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
                    if (baseLevelParam != null && baseLevelParam.AsElementId() != ElementId.InvalidElementId)
                    {
                        var baseLevel = _doc.GetElement(baseLevelParam.AsElementId()) as Level;
                        if (baseLevel != null && BaseLevelComboBox != null)
                        {
                            BaseLevelComboBox.SelectedItem = baseLevel;
                        }
                    }

                    // Set base offset
                    var baseOffsetParam = firstWall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET);
                    if (baseOffsetParam != null && BaseOffsetTextBox != null)
                    {
                        double offsetInFeet = baseOffsetParam.AsDouble();
                        BaseOffsetTextBox.Text = (offsetInFeet * 304.8).ToString("F2"); // Convert to mm
                    }
                }
                catch (Exception)
                {
                    // Handle cases where parameters might not be available
                }

                // Set top level
                try
                {
                    var topLevelParam = firstWall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
                    if (topLevelParam != null && topLevelParam.AsElementId() != ElementId.InvalidElementId)
                    {
                        var topLevel = _doc.GetElement(topLevelParam.AsElementId()) as Level;
                        if (topLevel != null && TopLevelComboBox != null)
                        {
                            TopLevelComboBox.SelectedItem = topLevel;
                        }
                    }

                    // Set top offset
                    var topOffsetParam = firstWall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET);
                    if (topOffsetParam != null && TopOffsetTextBox != null)
                    {
                        double offsetInFeet = topOffsetParam.AsDouble();
                        TopOffsetTextBox.Text = (offsetInFeet * 304.8).ToString("F2"); // Convert to mm
                    }
                }
                catch (Exception)
                {
                    // Handle cases where parameters might not be available
                }

                // Set current height
                try
                {
                    var heightParam = firstWall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
                    if (heightParam != null && CurrentHeightTextBox != null)
                    {
                        double heightInFeet = heightParam.AsDouble();
                        CurrentHeightTextBox.Text = (heightInFeet * 304.8).ToString("F2"); // Convert to mm
                    }
                }
                catch (Exception)
                {
                    // Handle cases where parameter might not be available
                    if (CurrentHeightTextBox != null)
                    {
                        CurrentHeightTextBox.Text = "N/A";
                    }
                }
            }
        }

        private void UpdateWallCountDisplay()
        {
            if (WallCountTextBlock != null)
            {
                WallCountTextBlock.Text = $"Selected Walls: {_walls.Count}";
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (AdjustBaseLevelCheckBox != null) AdjustBaseLevelCheckBox.IsChecked = true;
            if (AdjustTopLevelCheckBox != null) AdjustTopLevelCheckBox.IsChecked = true;
            if (AdjustHeightCheckBox != null) AdjustHeightCheckBox.IsChecked = true;
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (AdjustBaseLevelCheckBox != null) AdjustBaseLevelCheckBox.IsChecked = false;
            if (AdjustTopLevelCheckBox != null) AdjustTopLevelCheckBox.IsChecked = false;
            if (AdjustHeightCheckBox != null) AdjustHeightCheckBox.IsChecked = false;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInputs())
            {
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateInputs()
        {
            // Check if at least one adjustment is selected
            bool hasAdjustment = (AdjustBaseLevelCheckBox?.IsChecked == true) ||
                               (AdjustTopLevelCheckBox?.IsChecked == true) ||
                               (AdjustHeightCheckBox?.IsChecked == true);

            if (!hasAdjustment)
            {
                MessageBox.Show("Please select at least one adjustment option.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Validate base level selection
            if (AdjustBaseLevelCheckBox?.IsChecked == true && BaseLevelComboBox?.SelectedItem == null)
            {
                MessageBox.Show("Please select a base level.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Validate base offset
            if (AdjustBaseLevelCheckBox?.IsChecked == true && !string.IsNullOrWhiteSpace(BaseOffsetTextBox?.Text))
            {
                if (!double.TryParse(BaseOffsetTextBox.Text, out _))
                {
                    MessageBox.Show("Please enter a valid base offset value.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            // Validate top level selection
            if (AdjustTopLevelCheckBox?.IsChecked == true && TopLevelComboBox?.SelectedItem == null)
            {
                MessageBox.Show("Please select a top level.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Validate top offset
            if (AdjustTopLevelCheckBox?.IsChecked == true && !string.IsNullOrWhiteSpace(TopOffsetTextBox?.Text))
            {
                if (!double.TryParse(TopOffsetTextBox.Text, out _))
                {
                    MessageBox.Show("Please enter a valid top offset value.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            // Validate height adjustment
            if (AdjustHeightCheckBox?.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(HeightAdjustmentTextBox?.Text))
                {
                    MessageBox.Show("Please enter a height adjustment value.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (!double.TryParse(HeightAdjustmentTextBox.Text, out _))
                {
                    MessageBox.Show("Please enter a valid height adjustment value.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            return true;
        }

        public WallAdjustmentData GetAdjustmentData()
        {
            var data = new WallAdjustmentData();

            // Base level adjustment
            data.AdjustBaseLevel = AdjustBaseLevelCheckBox?.IsChecked == true;
            if (data.AdjustBaseLevel)
            {
                data.BaseLevel = BaseLevelComboBox?.SelectedItem as Level;

                if (double.TryParse(BaseOffsetTextBox?.Text, out double baseOffset))
                {
                    data.BaseOffset = baseOffset / 304.8; // Convert mm to feet
                }
            }

            // Top level adjustment
            data.AdjustTopLevel = AdjustTopLevelCheckBox?.IsChecked == true;
            if (data.AdjustTopLevel)
            {
                data.TopLevel = TopLevelComboBox?.SelectedItem as Level;

                if (double.TryParse(TopOffsetTextBox?.Text, out double topOffset))
                {
                    data.TopOffset = topOffset / 304.8; // Convert mm to feet
                }
            }

            // Height adjustment
            data.AdjustHeight = AdjustHeightCheckBox?.IsChecked == true;
            if (data.AdjustHeight)
            {
                if (double.TryParse(HeightAdjustmentTextBox?.Text, out double heightAdjustment))
                {
                    data.HeightAdjustment = heightAdjustment / 304.8; // Convert mm to feet
                }
            }

            return data;
        }

        private void AdjustBaseLevelCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (BaseLevelPanel != null)
                BaseLevelPanel.IsEnabled = true;
        }

        private void AdjustBaseLevelCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (BaseLevelPanel != null)
                BaseLevelPanel.IsEnabled = false;
        }

        private void AdjustTopLevelCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (TopLevelPanel != null)
                TopLevelPanel.IsEnabled = true;
        }

        private void AdjustTopLevelCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (TopLevelPanel != null)
                TopLevelPanel.IsEnabled = false;
        }

        private void AdjustHeightCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (HeightPanel != null)
                HeightPanel.IsEnabled = true;
        }

        private void AdjustHeightCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (HeightPanel != null)
                HeightPanel.IsEnabled = false;
        }

        private void ResetToDefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset all checkboxes
            if (AdjustBaseLevelCheckBox != null) AdjustBaseLevelCheckBox.IsChecked = false;
            if (AdjustTopLevelCheckBox != null) AdjustTopLevelCheckBox.IsChecked = false;
            if (AdjustHeightCheckBox != null) AdjustHeightCheckBox.IsChecked = false;

            // Reset offset values
            if (BaseOffsetTextBox != null) BaseOffsetTextBox.Text = "0";
            if (TopOffsetTextBox != null) TopOffsetTextBox.Text = "0";
            if (HeightAdjustmentTextBox != null) HeightAdjustmentTextBox.Text = "0";

            // Reset to initial level selections
            SetInitialValues();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set focus to the first checkbox when window loads
            AdjustBaseLevelCheckBox?.Focus();
        }
    }
}