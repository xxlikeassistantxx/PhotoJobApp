using PhotoJobApp.Models;
using PhotoJobApp.Services;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace PhotoJobApp;

[QueryProperty(nameof(JobTypeId), "JobType")]
public partial class AddEditJobTypePage : ContentPage
{
    private JobTypeService _jobTypeService;
    private JobType _jobType;
    private bool _isEditing = false;

    public string JobTypeId
    {
        set
        {
            if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int id))
            {
                _isEditing = true;
                LoadJobTypeAsync(id);
            }
        }
    }

    public AddEditJobTypePage()
    {
        InitializeComponent();
        ClearForm();
    }

    private void ClearForm()
    {
        _isEditing = false;
        _jobType = new JobType
        {
            CustomFieldsList = new ObservableCollection<CustomField>(),
            HasPhotos = true,
            HasLocation = true,
            HasClientInfo = true,
            HasPricing = true,
            HasDueDate = true,
            HasStatus = true,
            HasNotes = true,
            HasUrgentFlag = true,
            StatusOptions = "Pending,In Progress,Completed,Cancelled",
            Color = "#512BD4" // Default color
        };
        BindingContext = _jobType;
        Title = "New Job Type";
        UpdateColorSelection(_jobType.Color);
    }

    private async void LoadJobTypeAsync(int jobTypeId)
    {
        try
        {
                    if (_jobTypeService == null)
        {
            // Get current user for cloud sync
            var authService = new FirebaseAuthService();
            var currentUser = await authService.GetCurrentUserAsync();
            var userId = currentUser?.Id;
            
            _jobTypeService = await JobTypeService.CreateAsync(userId);
        }

            var jobType = await _jobTypeService.GetJobTypeAsync(jobTypeId);
            if (jobType != null)
            {
                _jobType = jobType;
                _isEditing = true;
                if (string.IsNullOrEmpty(_jobType.CustomFields))
                {
                    _jobType.CustomFieldsList = new ObservableCollection<CustomField>();
                }
                else
                {
                    try
                    {
                        _jobType.CustomFieldsList = new ObservableCollection<CustomField>(
                            JsonSerializer.Deserialize<List<CustomField>>(_jobType.CustomFields)
                        );
                    }
                    catch (JsonException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error deserializing custom fields: {ex.Message}");
                        _jobType.CustomFieldsList = new ObservableCollection<CustomField>();
                    }
                }
                BindingContext = _jobType;
                Title = "Edit Job Type";
                UpdateColorSelection(_jobType.Color);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load job type: {ex.Message}", "OK");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
        if (_jobTypeService == null)
        {
            // Get current user for cloud sync
            var authService = new FirebaseAuthService();
            var currentUser = await authService.GetCurrentUserAsync();
            var userId = currentUser?.Id;
            
            _jobTypeService = await JobTypeService.CreateAsync(userId);
            }
            
            // If not editing, ensure form is cleared for new job type entry
            if (!_isEditing)
            {
                ClearForm();
            }
            else
            {
                // Update color selection if editing
                if (_jobType != null && !string.IsNullOrEmpty(_jobType.Color))
                {
                    UpdateColorSelection(_jobType.Color);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing JobTypeService in OnAppearing: {ex.Message}");
            Console.WriteLine($"Error initializing JobTypeService in OnAppearing: {ex.Message}");
            // Try to create service without userId as fallback
            try
            {
                _jobTypeService = await JobTypeService.CreateAsync(null);
            }
            catch (Exception fallbackEx)
            {
                System.Diagnostics.Debug.WriteLine($"Fallback JobTypeService creation failed: {fallbackEx.Message}");
                Console.WriteLine($"Fallback JobTypeService creation failed: {fallbackEx.Message}");
            }
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("OnSaveClicked called");
        Console.WriteLine("OnSaveClicked called");
        
        if (string.IsNullOrWhiteSpace(_jobType.Name))
        {
            System.Diagnostics.Debug.WriteLine("Validation failed: Job type name is empty");
            Console.WriteLine("Validation failed: Job type name is empty");
            await DisplayAlert("Validation Error", "Job type name is required.", "OK");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"Job type name: {_jobType.Name}");
        Console.WriteLine($"Job type name: {_jobType.Name}");
        System.Diagnostics.Debug.WriteLine($"Job type description: {_jobType.Description}");
        Console.WriteLine($"Job type description: {_jobType.Description}");
        System.Diagnostics.Debug.WriteLine($"Custom fields count: {_jobType.CustomFieldsList?.Count ?? 0}");
        Console.WriteLine($"Custom fields count: {_jobType.CustomFieldsList?.Count ?? 0}");
        System.Diagnostics.Debug.WriteLine($"HasPhotos: {_jobType.HasPhotos}");
        Console.WriteLine($"HasPhotos: {_jobType.HasPhotos}");
        System.Diagnostics.Debug.WriteLine($"HasLocation: {_jobType.HasLocation}");
        Console.WriteLine($"HasLocation: {_jobType.HasLocation}");
        System.Diagnostics.Debug.WriteLine($"HasClientInfo: {_jobType.HasClientInfo}");
        Console.WriteLine($"HasClientInfo: {_jobType.HasClientInfo}");
        System.Diagnostics.Debug.WriteLine($"HasPricing: {_jobType.HasPricing}");
        Console.WriteLine($"HasPricing: {_jobType.HasPricing}");
        System.Diagnostics.Debug.WriteLine($"HasDueDate: {_jobType.HasDueDate}");
        Console.WriteLine($"HasDueDate: {_jobType.HasDueDate}");
        System.Diagnostics.Debug.WriteLine($"HasStatus: {_jobType.HasStatus}");
        Console.WriteLine($"HasStatus: {_jobType.HasStatus}");
        System.Diagnostics.Debug.WriteLine($"HasNotes: {_jobType.HasNotes}");
        Console.WriteLine($"HasNotes: {_jobType.HasNotes}");
        System.Diagnostics.Debug.WriteLine($"HasUrgentFlag: {_jobType.HasUrgentFlag}");
        Console.WriteLine($"HasUrgentFlag: {_jobType.HasUrgentFlag}");
        System.Diagnostics.Debug.WriteLine($"StatusOptions: {_jobType.StatusOptions}");
        Console.WriteLine($"StatusOptions: {_jobType.StatusOptions}");

        try
        {
            System.Diagnostics.Debug.WriteLine("Serializing custom fields...");
            Console.WriteLine("Serializing custom fields...");
            _jobType.CustomFields = JsonSerializer.Serialize(_jobType.CustomFieldsList);
            System.Diagnostics.Debug.WriteLine($"Serialized custom fields: {_jobType.CustomFields}");
            Console.WriteLine($"Serialized custom fields: {_jobType.CustomFields}");
            
            System.Diagnostics.Debug.WriteLine("Saving job type to database...");
            Console.WriteLine("Saving job type to database...");
            var result = await _jobTypeService.SaveJobTypeAsync(_jobType);
            System.Diagnostics.Debug.WriteLine($"Save result: {result}");
            Console.WriteLine($"Save result: {result}");
            
            await DisplayAlert("Success", "Job type saved successfully!", "OK");
            
            if (_isEditing)
            {
                // When editing, navigate back to MainPage
                try
                {
                    await Shell.Current.GoToAsync("///MainPage");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Shell navigation failed after save: {ex.Message}");
                Console.WriteLine($"Shell navigation failed after save: {ex.Message}");
                    await DisplayAlert("Navigation Error", "Job type saved but unable to navigate. Please use the menu to go back.", "OK");
                        }
                    }
            else
            {
                // When creating a new job type, clear the form to allow creating another job type
                ClearForm();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save job type: {ex.Message}", "OK");
        }
    }

            private async void OnCancelClicked(object sender, EventArgs e)
        {
            try
            {
                // Navigate to MainPage
                await Shell.Current.GoToAsync("///MainPage");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Shell navigation failed: {ex.Message}");
                Console.WriteLine($"Shell navigation failed: {ex.Message}");
                await DisplayAlert("Navigation Error", "Unable to go back. Please use the menu to navigate.", "OK");
            }
        }

    private async void OnAddCustomFieldClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("OnAddCustomFieldClicked called");
        Console.WriteLine("OnAddCustomFieldClicked called");
        
        string name = await DisplayPromptAsync("New Custom Field", "Enter field name:");
        if (string.IsNullOrWhiteSpace(name))
        {
            System.Diagnostics.Debug.WriteLine("Custom field name was empty, returning");
            Console.WriteLine("Custom field name was empty, returning");
            return;
        }

        string type = await DisplayActionSheet("Select Field Type", "Cancel", null, "Text", "Number", "Date", "Boolean");
        if (type == "Cancel" || type == null)
        {
            System.Diagnostics.Debug.WriteLine("Custom field type selection was cancelled");
            Console.WriteLine("Custom field type selection was cancelled");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"Adding custom field: {name} of type {type}");
        Console.WriteLine($"Adding custom field: {name} of type {type}");
        
        var newField = new CustomField { Name = name, Type = type, Required = false };
        _jobType.CustomFieldsList.Add(newField);
        
        System.Diagnostics.Debug.WriteLine($"Custom fields count after adding: {_jobType.CustomFieldsList.Count}");
        Console.WriteLine($"Custom fields count after adding: {_jobType.CustomFieldsList.Count}");
    }

    private void OnDeleteCustomFieldClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("OnDeleteCustomFieldClicked called");
        Console.WriteLine("OnDeleteCustomFieldClicked called");
        
        if (sender is Button button && button.CommandParameter is CustomField customField)
        {
            System.Diagnostics.Debug.WriteLine($"Removing custom field: {customField.Name}");
            Console.WriteLine($"Removing custom field: {customField.Name}");
            _jobType.CustomFieldsList.Remove(customField);
            
            System.Diagnostics.Debug.WriteLine($"Custom fields count after removing: {_jobType.CustomFieldsList.Count}");
            Console.WriteLine($"Custom fields count after removing: {_jobType.CustomFieldsList.Count}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("Delete custom field: Invalid parameters");
            Console.WriteLine("Delete custom field: Invalid parameters");
        }
    }

    private void OnColorSelected(object sender, TappedEventArgs e)
    {
        if (sender is Border border && border.Background is SolidColorBrush brush)
        {
            // Convert Color to hex string
            var color = brush.Color;
            var colorHex = $"#{(int)(color.Red * 255):X2}{(int)(color.Green * 255):X2}{(int)(color.Blue * 255):X2}";
            _jobType.Color = colorHex;
            UpdateColorSelection(colorHex);
        }
    }

    private void UpdateColorSelection(string selectedColor)
    {
        // Hide all checkmarks
        var colorChecks = new[] { 
            "Color1Check", "Color2Check", "Color3Check", "Color4Check", "Color5Check", "Color6Check",
            "Color7Check", "Color8Check", "Color9Check", "Color10Check", "Color11Check", "Color12Check"
        };
        var colorValues = new[] { 
            "#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4", "#FFEAA7", "#DDA0DD",
            "#512BD4", "#2196F3", "#4CAF50", "#FF9800", "#E91E63", "#9E9E9E"
        };
        
        for (int i = 0; i < colorChecks.Length; i++)
        {
            var checkLabel = this.FindByName<Label>(colorChecks[i]);
            if (checkLabel != null)
            {
                checkLabel.IsVisible = (colorValues[i].Equals(selectedColor, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
} 