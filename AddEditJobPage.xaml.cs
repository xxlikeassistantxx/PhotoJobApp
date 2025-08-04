using PhotoJobApp.Models;
using PhotoJobApp.Services;
using System.Collections.ObjectModel;

namespace PhotoJobApp
{
    [QueryProperty(nameof(JobId), "Job")]
    [QueryProperty(nameof(JobTypeId), "JobTypeId")]
    public partial class AddEditJobPage : ContentPage
    {
        private readonly PhotoJobService _photoJobService;
        private JobTypeService _jobTypeService;
        private PhotoJob _job;
        private bool _isEditing;
        public ObservableCollection<string> Photos { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<JobType> AvailableJobTypes { get; set; } = new ObservableCollection<JobType>();
        public JobType SelectedJobType { get; set; }
        
        public PhotoJob Job => _job;

        public string JobId
        {
            set
            {
                if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int id))
                {
                    LoadJobAsync(id);
                }
            }
        }

        public string JobTypeId
        {
            set
            {
                if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int id))
                {
                    _preSelectedJobTypeId = id;
                }
            }
        }

        private int _preSelectedJobTypeId = 0;

        private async Task InitializeJobTypeServiceAsync()
        {
            try
            {
                // Get current user for cloud sync
                var authService = new FirebaseAuthService();
                var currentUser = await authService.GetCurrentUserAsync();
                var userId = currentUser?.Id;
                
                _jobTypeService = await JobTypeService.CreateAsync(userId);
                LoadJobTypesAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to initialize job type service: {ex.Message}", "OK");
            }
        }

        public AddEditJobPage()
        {
            InitializeComponent();
            _photoJobService = new PhotoJobService();
            _job = new PhotoJob();
            _isEditing = false;
            Title = "Add New Job";
            BindingContext = this;
            _ = InitializeJobTypeServiceAsync();
            
            // Apply theme
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            try
            {
                // Apply default theme
                ThemeService.Instance.ApplyThemeToPage(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
            }
        }

        private void UpdateThemeFromJobType()
        {
            if (SelectedJobType != null)
            {
                try
                {
                    // Update theme colors from the selected job type
                    ThemeService.Instance.UpdateThemeFromJobType(SelectedJobType);
                    
                    // Apply the updated theme with custom background color
                    var jobTypeColor = Microsoft.Maui.Graphics.Color.FromArgb(SelectedJobType.Color);
                    var darkerBackground = ThemeService.Instance.GetLighterColor(jobTypeColor, 0.7f);
                    ThemeService.Instance.ApplyThemeToPage(this, darkerBackground);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating theme from job type: {ex.Message}");
                }
            }
        }

        private async void LoadJobTypesAsync()
        {
            try
            {
                if (_jobTypeService == null) return;
                
                var jobTypes = await _jobTypeService.GetJobTypesAsync();
                AvailableJobTypes.Clear();
                foreach (var jobType in jobTypes)
                {
                    AvailableJobTypes.Add(jobType);
                }
                
                // Set the picker's items source
                JobTypePicker.ItemsSource = AvailableJobTypes;
                JobTypePicker.ItemDisplayBinding = new Binding("Name");
                
                // If there's only one job type, select it automatically
                if (AvailableJobTypes.Count == 1)
                {
                    SelectedJobType = AvailableJobTypes[0];
                    JobTypePicker.SelectedItem = SelectedJobType;
                    UpdateFormVisibility();
                    OnJobTypeChanged(null, null);
                }
                else if (AvailableJobTypes.Count == 0)
                {
                    // Create a default job type if none exist
                    await CreateDefaultJobType();
                }
                else if (_preSelectedJobTypeId > 0)
                {
                    // Pre-select the job type if one was passed
                    var preSelectedType = AvailableJobTypes.FirstOrDefault(jt => jt.Id == _preSelectedJobTypeId);
                    if (preSelectedType != null)
                    {
                        SelectedJobType = preSelectedType;
                        JobTypePicker.SelectedItem = SelectedJobType;
                        UpdateFormVisibility();
                        OnJobTypeChanged(null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load job types: {ex.Message}", "OK");
            }
        }

        private void OnJobTypeChanged(object sender, EventArgs e)
        {
            if (JobTypePicker.SelectedItem is JobType selectedType)
            {
                SelectedJobType = selectedType;
                
                // Update the status picker with the job type's status options
                SetupStatusPicker();
                
                // Show/hide sections based on job type configuration
                UpdateFormVisibility();
                
                // Update theme from the selected job type
                UpdateThemeFromJobType();
                
                // Force UI update
                OnPropertyChanged(nameof(SelectedJobType));
            }
        }

        private void UpdateFormVisibility()
        {
            if (SelectedJobType == null) return;

            // Show/hide sections based on job type settings
            ClientInfoSection.IsVisible = SelectedJobType.HasClientInfo;
            PriceSection.IsVisible = SelectedJobType.HasPricing;
            StatusSection.IsVisible = SelectedJobType.HasStatus;
            DueDateSection.IsVisible = SelectedJobType.HasDueDate;
            LocationSection.IsVisible = SelectedJobType.HasLocation;
            PhotosSection.IsVisible = SelectedJobType.HasPhotos;
            NotesSection.IsVisible = SelectedJobType.HasNotes;
            UrgentSection.IsVisible = SelectedJobType.HasUrgentFlag;
            
            // Handle custom fields
            LoadCustomFields();
        }

        private void LoadCustomFields()
        {
            try
            {
                if (SelectedJobType == null) return;

                // Load custom fields from JSON string
                if (!string.IsNullOrEmpty(SelectedJobType.CustomFields))
                {
                    var customFields = System.Text.Json.JsonSerializer.Deserialize<List<CustomField>>(SelectedJobType.CustomFields);
                    if (customFields != null && customFields.Count > 0)
                    {
                        CustomFieldsSection.IsVisible = true;
                        CustomFieldsCollectionView.ItemsSource = customFields;
                        return;
                    }
                }

                // If no custom fields, hide the section
                CustomFieldsSection.IsVisible = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading custom fields: {ex.Message}");
                CustomFieldsSection.IsVisible = false;
            }
        }

        protected override bool OnBackButtonPressed()
        {
            try
            {
                // Use the same navigation logic as OnCancelClicked
                OnCancelClicked(null, null);
                return true; // Prevent default back button behavior
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnBackButtonPressed error: {ex.Message}");
                Console.WriteLine($"OnBackButtonPressed error: {ex.Message}");
                return false; // Allow default behavior if our navigation fails
            }
        }

        private async void LoadJobAsync(int jobId)
        {
            try
            {
                var job = await _photoJobService.GetJobAsync(jobId);
                if (job != null)
                {
                    _job = job;
                    _isEditing = true;
                    Title = "Edit Job";
                    BindingContext = this;
                    
                    // Initialize job type service if not already done
                    if (_jobTypeService == null)
                    {
                        await InitializeJobTypeServiceAsync();
                    }
                    else
                    {
                        LoadJobTypesAsync();
                    }
                    
                    // Find and select the job's type
                    if (job.JobTypeId > 0)
                    {
                        var jobType = AvailableJobTypes.FirstOrDefault(jt => jt.Id == job.JobTypeId);
                        if (jobType != null)
                        {
                            SelectedJobType = jobType;
                            JobTypePicker.SelectedItem = SelectedJobType;
                            UpdateFormVisibility();
                        }
                    }
                    
                    SetupStatusPicker();
                    
                    // Load existing photos
                    Photos.Clear();
                    foreach (var photo in _job.PhotoList)
                    {
                        Photos.Add(photo);
                    }

                    // Load custom field values if editing
                    if (_isEditing)
                    {
                        LoadCustomFieldValues();
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load job: {ex.Message}", "OK");
            }
        }

        private void SetupStatusPicker()
        {
            if (SelectedJobType != null)
            {
                StatusPicker.ItemsSource = SelectedJobType.StatusList;
                StatusPicker.SelectedItem = _job.Status ?? SelectedJobType.StatusList.FirstOrDefault();
            }
            else
            {
                StatusPicker.ItemsSource = new List<string> { "Pending", "In Progress", "Completed", "Cancelled" };
                StatusPicker.SelectedItem = _job.Status ?? "Pending";
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (ValidateForm())
            {
                try
                {
                    // Save the selected job type ID
                    if (SelectedJobType != null)
                    {
                        _job.JobTypeId = SelectedJobType.Id;
                    }
                    
                    // Update status from picker
                    _job.Status = StatusPicker.SelectedItem?.ToString() ?? "Pending";
                    
                    // Update due date
                    _job.DueDate = DueDatePicker.Date;
                    
                    // Save photos
                    _job.PhotoList = Photos.ToList();

                    // Save custom field values
                    SaveCustomFieldValues();

                    await _photoJobService.SaveJobAsync(_job);
                    
                    string message = _isEditing ? "Job updated successfully!" : "Job added successfully!";
                    await DisplayAlert("Success", message, "OK");
                    
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
                                await DisplayAlert("Navigation Error", "Job saved but unable to go back. Please restart the app.", "OK");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to save job: {ex.Message}", "OK");
                }
            }
        }

        private void SaveCustomFieldValues()
        {
            try
            {
                if (SelectedJobType == null || string.IsNullOrEmpty(SelectedJobType.CustomFields)) return;

                var customFields = System.Text.Json.JsonSerializer.Deserialize<List<CustomField>>(SelectedJobType.CustomFields);
                if (customFields == null || customFields.Count == 0) return;

                var customFieldValues = new Dictionary<string, string>();
                
                // For now, we'll save the default values
                // In a more advanced implementation, you'd collect the actual values from the UI
                foreach (var field in customFields)
                {
                    customFieldValues[field.Name] = field.DefaultValue;
                }

                _job.CustomFieldValues = System.Text.Json.JsonSerializer.Serialize(customFieldValues);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving custom field values: {ex.Message}");
            }
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(_job.Title))
            {
                DisplayAlert("Validation Error", "Please enter a job title.", "OK");
                TitleEntry.Focus();
                return false;
            }

            // Validate job type selection
            if (SelectedJobType == null)
            {
                DisplayAlert("Validation Error", "Please select a job type.", "OK");
                JobTypePicker.Focus();
                return false;
            }

            // Validate client name only if job type has client info enabled
            if (SelectedJobType.HasClientInfo && string.IsNullOrWhiteSpace(_job.ClientName))
            {
                DisplayAlert("Validation Error", "Please enter a client name.", "OK");
                ClientNameEntry.Focus();
                return false;
            }

            // Validate price only if job type has pricing enabled
            if (SelectedJobType.HasPricing && _job.Price <= 0)
            {
                DisplayAlert("Validation Error", "Please enter a valid price greater than 0.", "OK");
                PriceEntry.Focus();
                return false;
            }

            return true;
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

        private async void OnAddPhotoClicked(object sender, EventArgs e)
        {
            try
            {
                // Show options for camera or gallery
                var action = await DisplayActionSheet("Add Photo", "Cancel", null, "Take Photo", "Choose from Gallery");
                
                if (action == "Cancel") return;
                
                FileResult? photo = null;
                
                if (action == "Take Photo")
                {
                    // Check if camera is available
                    if (!MediaPicker.IsCaptureSupported)
                    {
                        await DisplayAlert("Camera Not Available", "Camera is not available on this device.", "OK");
                        return;
                    }
                    
                    photo = await MediaPicker.CapturePhotoAsync();
                }
                else if (action == "Choose from Gallery")
                {
                    photo = await MediaPicker.PickPhotoAsync();
                }
                
                if (photo != null)
                {
                    var localPath = Path.Combine(FileSystem.AppDataDirectory, $"photo_{DateTime.Now.Ticks}.jpg");
                    using (var stream = await photo.OpenReadAsync())
                    using (var newStream = File.OpenWrite(localPath))
                    {
                        await stream.CopyToAsync(newStream);
                    }
                    
                    Photos.Add(localPath);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to add photo: {ex.Message}", "OK");
            }
        }

        private void OnRemovePhotoClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string photoPath)
            {
                Photos.Remove(photoPath);
                
                // Delete the file
                try
                {
                    if (File.Exists(photoPath))
                    {
                        File.Delete(photoPath);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to delete photo file: {ex.Message}");
                }
            }
        }

        private async Task CreateDefaultJobType()
        {
            try
            {
                var defaultJobType = new JobType
                {
                    Name = "Default Job Type",
                    Description = "A default job type for basic jobs",
                    HasPhotos = false,
                    HasLocation = false,
                    HasClientInfo = false,
                    HasPricing = false,
                    HasDueDate = false,
                    HasStatus = false,
                    HasNotes = false,
                    HasUrgentFlag = false,
                    StatusOptions = "Pending,Completed",
                    Icon = "📋",
                    Color = "#512BD4"
                };

                await _jobTypeService.SaveJobTypeAsync(defaultJobType);
                
                // Reload job types
                LoadJobTypesAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to create default job type: {ex.Message}", "OK");
            }
        }

        private void LoadCustomFieldValues()
        {
            try
            {
                if (string.IsNullOrEmpty(_job.CustomFieldValues)) return;

                var customFieldValues = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(_job.CustomFieldValues);
                if (customFieldValues == null) return;

                // Update the custom fields with the saved values
                if (CustomFieldsCollectionView.ItemsSource is List<CustomField> customFields)
                {
                    foreach (var field in customFields)
                    {
                        if (customFieldValues.ContainsKey(field.Name))
                        {
                            field.DefaultValue = customFieldValues[field.Name];
                        }
                    }
                    
                    // Refresh the collection view
                    CustomFieldsCollectionView.ItemsSource = null;
                    CustomFieldsCollectionView.ItemsSource = customFields;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading custom field values: {ex.Message}");
            }
        }
    }
} 