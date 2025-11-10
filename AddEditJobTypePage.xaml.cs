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

    public string JobTypeId
    {
        set
        {
            if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int id))
            {
                LoadJobTypeAsync(id);
            }
        }
    }

    public AddEditJobTypePage()
    {
        InitializeComponent();
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
            StatusOptions = "Pending,In Progress,Completed,Cancelled"
        };
        BindingContext = _jobType;
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
        if (_jobTypeService == null)
        {
            // Get current user for cloud sync
            var authService = new FirebaseAuthService();
            var currentUser = await authService.GetCurrentUserAsync();
            var userId = currentUser?.Id;
            
            _jobTypeService = await JobTypeService.CreateAsync(userId);
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
            
            try
            {
                // Try Shell navigation first
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Shell navigation failed after save: {ex.Message}");
                Console.WriteLine($"Shell navigation failed after save: {ex.Message}");
                
                // Fallback: Create a new AppShell and set it as the window page
                try
                {
                    if (Application.Current.Windows.Count > 0)
                    {
                        var authService = new FirebaseAuthService();
                        var appShell = new AppShell(authService);
                        Application.Current.Windows[0].Page = appShell;
                        
                        System.Diagnostics.Debug.WriteLine("Save navigation completed via fallback");
                        Console.WriteLine("Save navigation completed via fallback");
                    }
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Save fallback navigation failed: {fallbackEx.Message}");
                    Console.WriteLine($"Save fallback navigation failed: {fallbackEx.Message}");
                    
                    // Final fallback: Go back to MainPage
                    try
                    {
                        if (Application.Current.Windows.Count > 0)
                        {
                            var mainPage = new MainPage();
                            Application.Current.Windows[0].Page = mainPage;
                            
                            System.Diagnostics.Debug.WriteLine("Save navigation completed via MainPage fallback");
                            Console.WriteLine("Save navigation completed via MainPage fallback");
                        }
                    }
                    catch (Exception finalEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Save final fallback navigation failed: {finalEx.Message}");
                        Console.WriteLine($"Save final fallback navigation failed: {finalEx.Message}");
                        await DisplayAlert("Navigation Error", "Job type saved but unable to go back. Please restart the app.", "OK");
                    }
                }
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
                // Try Shell navigation first
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Shell navigation failed: {ex.Message}");
                Console.WriteLine($"Shell navigation failed: {ex.Message}");
                
                // Fallback: Create a new AppShell and set it as the window page
                try
                {
                    if (Application.Current.Windows.Count > 0)
                    {
                        var authService = new FirebaseAuthService();
                        var appShell = new AppShell(authService);
                        Application.Current.Windows[0].Page = appShell;
                        
                        System.Diagnostics.Debug.WriteLine("Back navigation completed via fallback");
                        Console.WriteLine("Back navigation completed via fallback");
                    }
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Fallback navigation failed: {fallbackEx.Message}");
                    Console.WriteLine($"Fallback navigation failed: {fallbackEx.Message}");
                    
                    // Final fallback: Go back to MainPage
                    try
                    {
                        if (Application.Current.Windows.Count > 0)
                        {
                            var mainPage = new MainPage();
                            Application.Current.Windows[0].Page = mainPage;
                            
                            System.Diagnostics.Debug.WriteLine("Back navigation completed via MainPage fallback");
                            Console.WriteLine("Back navigation completed via MainPage fallback");
                        }
                    }
                    catch (Exception finalEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Final fallback navigation failed: {finalEx.Message}");
                        Console.WriteLine($"Final fallback navigation failed: {finalEx.Message}");
                        await DisplayAlert("Navigation Error", "Unable to go back. Please restart the app.", "OK");
                    }
                }
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
} 