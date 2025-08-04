using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using LandscapeRevitAddIn.Utils;

namespace LandscapeRevitAddIn.UI.Windows.Panel08
{
    public partial class AnnotationSettingsWindow : Window
    {
        private readonly UIDocument _uiDocument;
        private readonly Document _document;
        private List<CategoryViewModel> _categories;

        public AnnotationSettings Settings { get; private set; }

        public AnnotationSettingsWindow(UIDocument uiDocument)
        {
            InitializeComponent();
            _uiDocument = uiDocument;
            _document = uiDocument.Document;
            
            LoadData();
        }

        private void LoadData()
        {
            LoadCategories();
            LoadTextTypes();
            UpdateSummary();
        }

        private void LoadCategories()
        {
            _categories = new List<CategoryViewModel>();
            
            // Add common categories for annotation
            var commonCategories = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Floors,
                BuiltInCategory.OST_Doors,
                BuiltInCategory.OST_Windows,
                BuiltInCategory.OST_Furniture,
                BuiltInCategory.OST_Planting,
                BuiltInCategory.OST_GenericModel,
                BuiltInCategory.OST_Columns,
                BuiltInCategory.OST_StructuralFraming,
                BuiltInCategory.OST_Roofs,
                BuiltInCategory.OST_Ceilings
            };

            foreach (var builtInCategory in commonCategories)
            {
                try
                {
                    Category category = _document.Settings.Categories.get_Item(builtInCategory);
                    if (category != null)
                    {
                        _categories.Add(new CategoryViewModel
                        {
                            Category = category,
                            Name = category.Name,
                            IsSelected = false
                        });
                    }
                }
                catch
                {
                    // Skip categories that don't exist in this document
                }
            }

            CategoriesListBox.ItemsSource = _categories.OrderBy(c => c.Name).ToList();
        }

        private void LoadTextTypes()
        {
            var textTypes = new FilteredElementCollector(_document)
                .OfClass(typeof(TextNoteType))
                .Cast<TextNoteType>()
                .OrderBy(t => t.Name)
                .ToList();

            TextTypeComboBox.ItemsSource = textTypes;
            
            if (textTypes.Count > 0)
            {
                TextTypeComboBox.SelectedIndex = 0;
            }
        }

        private void SelectAllCategoriesCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var category in _categories)
            {
                category.IsSelected = true;
            }
            CategoriesListBox.Items.Refresh();
            UpdateSummary();
        }

        private void SelectAllCategoriesCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var category in _categories)
            {
                category.IsSelected = false;
            }
            CategoriesListBox.Items.Refresh();
            UpdateSummary();
        }

        private void AddParameterButton_Click(object sender, RoutedEventArgs e)
        {
            string parameterName = NewParameterTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(parameterName))
            {
                if (!CustomParametersListBox.Items.Contains(parameterName))
                {
                    CustomParametersListBox.Items.Add(parameterName);
                    NewParameterTextBox.Clear();
                }
                else
                {
                    RevitUtils.ShowMessage("Warning", "Parameter already added to the list.");
                }
            }
        }

        private void RemoveParameterButton_Click(object sender, RoutedEventArgs e)
        {
            if (CustomParametersListBox.SelectedItem != null)
            {
                CustomParametersListBox.Items.Remove(CustomParametersListBox.SelectedItem);
            }
        }

        private void UpdateSummary()
        {
            int selectedCategories = _categories?.Count(c => c.IsSelected) ?? 0;
            
            if (selectedCategories > 0)
            {
                SummaryTextBlock.Text = $"Ready to create annotations for {selectedCategories} selected categor{(selectedCategories == 1 ? "y" : "ies")}";
            }
            else
            {
                SummaryTextBlock.Text = "Select categories to annotate and configure settings";
            }
        }

        private void CreateAnnotationsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate selections
                var selectedCategories = _categories.Where(c => c.IsSelected).ToList();
                if (selectedCategories.Count == 0)
                {
                    RevitUtils.ShowMessage("Warning", "Please select at least one category to annotate.");
                    return;
                }

                var selectedTextType = TextTypeComboBox.SelectedItem as TextNoteType;
                if (selectedTextType == null)
                {
                    RevitUtils.ShowMessage("Warning", "Please select a text type for the annotations.");
                    return;
                }

                // Create settings object
                Settings = new AnnotationSettings
                {
                    CategoryIds = selectedCategories.Select(c => c.Category.Id).ToList(),
                    TextTypeId = selectedTextType.Id,
                    IncludeElementId = IncludeIdCheckBox.IsChecked == true,
                    IncludeElementName = IncludeNameCheckBox.IsChecked == true,
                    IncludeCategory = IncludeCategoryCheckBox.IsChecked == true,
                    CustomParameters = CustomParametersListBox.Items.Cast<string>().ToList()
                };

                // Set arrangement
                if (LeftArrangementRadio.IsChecked == true)
                    Settings.Arrangement = AnnotationArrangement.Left;
                else if (RightArrangementRadio.IsChecked == true)
                    Settings.Arrangement = AnnotationArrangement.Right;
                else if (BothArrangementRadio.IsChecked == true)
                    Settings.Arrangement = AnnotationArrangement.Both;

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                RevitUtils.ShowMessage("Error", $"Failed to create annotation settings: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }

    // ViewModel for Category display with selection
    public class CategoryViewModel
    {
        public Category Category { get; set; }
        public string Name { get; set; }
        public bool IsSelected { get; set; }
    }

    // Settings class for annotation configuration
    public class AnnotationSettings
    {
        public List<ElementId> CategoryIds { get; set; } = new List<ElementId>();
        public AnnotationArrangement Arrangement { get; set; } = AnnotationArrangement.Right;
        public ElementId TextTypeId { get; set; }
        public bool IncludeElementId { get; set; } = true;
        public bool IncludeElementName { get; set; } = true;
        public bool IncludeCategory { get; set; } = true;
        public List<string> CustomParameters { get; set; } = new List<string>();
    }

    public enum AnnotationArrangement
    {
        Left,
        Right,
        Both
    }
}