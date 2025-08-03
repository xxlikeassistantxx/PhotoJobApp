using PhotoJobApp.Models;
using PhotoJobApp.Services;
using System.Collections.ObjectModel;

namespace PhotoJobApp
{
    public partial class AddEditJobTypePage : ContentPage
    {
        private readonly JobTypeService _jobTypeService;
        private JobType _jobType;
        private bool _isEditing;

        public AddEditJobTypePage(JobType? jobType = null)
        {
            InitializeComponent();
            _jobTypeService = new JobTypeService();
            
            if (jobType != null)
            {
                _jobType = jobType;
                _isEditing = true;
                Title = "Edit Job Type";
            }
            else
            {
                _jobType = new JobType();
                _isEditing = false;
                Title = "New Job Type";
            }
            
            BindingContext = _jobType;
        }

        private void OnColorPickerChanged(object sender, EventArgs e)
        {
            if (sender is Picker picker && picker.SelectedItem is string color)
            {
                _jobType.Color = color;
            }
        }

        private async void OnAddCustomFieldClicked(object sender, EventArgs e)
        {
            var fieldName = await DisplayPromptAsync("Add Custom Field", "Field Name:");
            if (string.IsNullOrWhiteSpace(fieldName))
                return;

            var fieldType = await DisplayActionSheet("Field Type", "Cancel", null, "text", "number", "date", "boolean", "dropdown");
            if (fieldType == "Cancel")
                return;

            var isRequired = await DisplayAlert("Required Field", "Is this field required?", "Yes", "No");

            var customField = new CustomField
            {
                Name = fieldName,
                Type = fieldType,
                Required = isRequired
            };

            if (fieldType == "dropdown")
            {
                var options = await DisplayPromptAsync("Dropdown Options", "Enter options (comma-separated):");
                if (!string.IsNullOrWhiteSpace(options))
                {
                    customField.Options = options;
                }
            }

            _jobType.CustomFieldsList.Add(customField);
        }

        private void OnDeleteCustomFieldClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is CustomField field)
            {
                _jobType.CustomFieldsList.Remove(field);
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_jobType.Name))
            {
                await DisplayAlert("Validation Error", "Please enter a name for the job type.", "OK");
                return;
            }

            try
            {
                await _jobTypeService.SaveJobTypeAsync(_jobType);
                await DisplayAlert("Success", 
                    _isEditing ? "Job type updated successfully!" : "Job type created successfully!", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to save job type: {ex.Message}", "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
} 