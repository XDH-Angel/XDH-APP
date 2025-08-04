using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using LandscapeRevitAddIn.Utils;

namespace LandscapeRevitAddIn.UI.Windows.Panel08
{
    public partial class ScopeBoxSelectionWindow : Window
    {
        private readonly UIDocument _uiDocument;
        private readonly Document _document;
        private List<ScopeBoxViewModel> _scopeBoxes;
        private List<FamilySymbol> _titleBlocks;

        public ScopeBoxSelectionSettings Settings { get; private set; }

        public ScopeBoxSelectionWindow(UIDocument uiDocument)
        {
            InitializeComponent();
            _uiDocument = uiDocument;
            _document = uiDocument.Document;
            
            LoadData();
        }

        private void LoadData()
        {
            LoadScopeBoxes();
            LoadTitleBlocks();
            UpdateUI();
        }

        private void LoadScopeBoxes()
        {
            _scopeBoxes = new List<ScopeBoxViewModel>();
            
            var scopeBoxElements = new FilteredElementCollector(_document)
                .OfCategory(BuiltInCategory.OST_VolumeOfInterest)
                .WhereElementIsNotElementType()
                .ToList();

            foreach (Element element in scopeBoxElements)
            {
                _scopeBoxes.Add(new ScopeBoxViewModel
                {
                    Element = element,
                    Name = element.Name,
                    Description = $"ID: {element.Id} | Level: {GetElementLevel(element)}",
                    IsSelected = false
                });
            }

            ScopeBoxesListBox.ItemsSource = _scopeBoxes.OrderBy(s => s.Name).ToList();
        }

        private string GetElementLevel(Element element)
        {
            try
            {
                Parameter levelParam = element.get_Parameter(BuiltInParameter.LEVEL_PARAM);
                if (levelParam != null && levelParam.AsElementId() != ElementId.InvalidElementId)
                {
                    Element level = _document.GetElement(levelParam.AsElementId());
                    return level?.Name ?? "Unknown";
                }
                return "N/A";
            }
            catch
            {
                return "N/A";
            }
        }

        private void LoadTitleBlocks()
        {
            _titleBlocks = new FilteredElementCollector(_document)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .WhereElementIsElementType()
                .Cast<FamilySymbol>()
                .OrderBy(tb => tb.Name)
                .ToList();

            TitleBlockComboBox.ItemsSource = _titleBlocks;
            
            if (_titleBlocks.Count > 0)
            {
                TitleBlockComboBox.SelectedIndex = 0;
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var scopeBox in _scopeBoxes)
            {
                scopeBox.IsSelected = true;
            }
            ScopeBoxesListBox.Items.Refresh();
            UpdateUI();
        }

        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var scopeBox in _scopeBoxes)
            {
                scopeBox.IsSelected = false;
            }
            ScopeBoxesListBox.Items.Refresh();
            UpdateUI();
        }

        private void ScopeBoxCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateUI();
        }

        private void ScopeBoxesListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateUI();
        }

        private void TitleBlockComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selectedTitleBlock = TitleBlockComboBox.SelectedItem as FamilySymbol;
            if (selectedTitleBlock != null)
            {
                TitleBlockInfoText.Text = $"Selected: {selectedTitleBlock.Name}";
            }
            UpdateUI();
        }

        private void UpdateUI()
        {
            int selectedCount = _scopeBoxes?.Count(s => s.IsSelected) ?? 0;
            
            SelectionCountText.Text = $"{selectedCount} scope box{(selectedCount == 1 ? "" : "es")} selected";
            
            bool canCreateSheets = selectedCount > 0 && TitleBlockComboBox.SelectedItem != null;
            CreateSheetsButton.IsEnabled = canCreateSheets;

            if (canCreateSheets)
            {
                SummaryTextBlock.Text = $"Ready to create {selectedCount} sheet{(selectedCount == 1 ? "" : "s")} from selected scope boxes";
            }
            else
            {
                SummaryTextBlock.Text = "Select scope boxes and title block to create sheets";
            }
        }

        private void CreateSheetsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedScopeBoxes = _scopeBoxes.Where(s => s.IsSelected).ToList();
                if (selectedScopeBoxes.Count == 0)
                {
                    RevitUtils.ShowMessage("Warning", "Please select at least one scope box.");
                    return;
                }

                var selectedTitleBlock = TitleBlockComboBox.SelectedItem as FamilySymbol;
                if (selectedTitleBlock == null)
                {
                    RevitUtils.ShowMessage("Warning", "Please select a title block type.");
                    return;
                }

                // Create settings object
                Settings = new ScopeBoxSelectionSettings
                {
                    SelectedScopeBoxes = selectedScopeBoxes.Select(s => s.Element).ToList(),
                    TitleBlock = selectedTitleBlock,
                    SheetNumberPrefix = SheetNumberPrefixTextBox.Text.Trim(),
                    IncludeScopeBoxNameInSheetName = IncludeScopeBoxNameCheckBox.IsChecked == true
                };

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                RevitUtils.ShowMessage("Error", $"Failed to create sheet settings: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }

    // ViewModel for ScopeBox display with selection
    public class ScopeBoxViewModel
    {
        public Element Element { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsSelected { get; set; }
    }

    // Settings class for scope box selection
    public class ScopeBoxSelectionSettings
    {
        public List<Element> SelectedScopeBoxes { get; set; } = new List<Element>();
        public FamilySymbol TitleBlock { get; set; }
        public string SheetNumberPrefix { get; set; } = "A";
        public bool IncludeScopeBoxNameInSheetName { get; set; } = true;
    }
}