using PhotoJobApp.Models;
using PhotoJobApp.Services;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace PhotoJobApp
{
    [QueryProperty(nameof(JobId), "Job")]
    [QueryProperty(nameof(PreSelectedJobTypeId), "JobTypeId")]
    public partial class AddEditJobPage : ContentPage
    {
        private readonly PhotoJobService _photoJobService;
        private JobTypeService? _jobTypeService;
        private PhotoJob _job;
        private bool _isEditing;
        private JobType? _selectedJobType;
        private Dictionary<string, View> _customFieldViews = new Dictionary<string, View>();
        private Dictionary<string, string> _customFieldValues = new Dictionary<string, string>();
        public ObservableCollection<string> Photos { get; set; } = new ObservableCollection<string>();
        
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

        public string PreSelectedJobTypeId
        {
            set
            {
                if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int jobTypeId))
                {
                    // Store the job type ID to load after OnAppearing
                    _preSelectedJobTypeId = jobTypeId;
                }
            }
        }

        private int? _preSelectedJobTypeId = null;

        public AddEditJobPage()
        {
            InitializeComponent();
            _photoJobService = new PhotoJobService();
            _job = new PhotoJob();
            _isEditing = false;
            Title = "Add New Job";
            BindingContext = this; // Changed to bind to this page for Photos collection
            
            // Always show job type picker for new jobs - user can change it anytime
            if (JobTypeLabel != null) JobTypeLabel.IsVisible = true;
            if (JobTypePicker != null) 
            {
                JobTypePicker.IsVisible = true;
                JobTypePicker.SelectedIndex = -1; // Start with no selection
            }
            
            // Hide all sections initially - they'll show when job type is selected
            if (ClientInfoSection != null) ClientInfoSection.IsVisible = false;
            if (PricingSection != null) PricingSection.IsVisible = false;
            if (StatusSection != null) StatusSection.IsVisible = false;
            if (DueDateSection != null) DueDateSection.IsVisible = false;
            if (LocationSection != null) LocationSection.IsVisible = false;
            if (PhotosSection != null) PhotosSection.IsVisible = false;
            if (NotesSection != null) NotesSection.IsVisible = false;
            if (UrgentSection != null) UrgentSection.IsVisible = false;
            if (CustomFieldsSection != null) CustomFieldsSection.IsVisible = false;
        }

        private async Task ClearFormAsync()
        {
            _job = new PhotoJob();
            _isEditing = false;
            _customFieldValues.Clear();
            _customFieldViews.Clear();
            Photos.Clear();
            _selectedJobType = null;
            _job.JobTypeId = 0;
            
            // Hide all sections initially (they'll be shown again when job type is selected)
            if (ClientInfoSection != null) ClientInfoSection.IsVisible = false;
            if (PricingSection != null) PricingSection.IsVisible = false;
            if (StatusSection != null) StatusSection.IsVisible = false;
            if (DueDateSection != null) DueDateSection.IsVisible = false;
            if (LocationSection != null) LocationSection.IsVisible = false;
            if (PhotosSection != null) PhotosSection.IsVisible = false;
            if (NotesSection != null) NotesSection.IsVisible = false;
            if (UrgentSection != null) UrgentSection.IsVisible = false;
            if (CustomFieldsSection != null) CustomFieldsSection.IsVisible = false;
            
            // Clear all entry fields
            if (TitleEntry != null) TitleEntry.Text = string.Empty;
            if (DescriptionEditor != null) 
            {
                DescriptionEditor.Text = string.Empty;
                DescriptionEditor.Placeholder = "Enter job description"; // Reset placeholder
            }
            if (ClientNameEntry != null) ClientNameEntry.Text = string.Empty;
            if (ClientPhoneEntry != null) ClientPhoneEntry.Text = string.Empty;
            if (ClientEmailEntry != null) ClientEmailEntry.Text = string.Empty;
            if (PriceEntry != null) PriceEntry.Text = string.Empty;
            if (LocationEntry != null) LocationEntry.Text = string.Empty;
            if (NotesEditor != null) NotesEditor.Text = string.Empty;
            if (UrgentCheckBox != null) UrgentCheckBox.IsChecked = false;
            if (DueDatePicker != null) DueDatePicker.Date = DateTime.Now;
            if (StatusPicker != null) 
            {
                StatusPicker.SelectedIndex = -1;
                StatusPicker.ItemsSource = null;
            }
            
            // Clear custom fields container
            if (CustomFieldsContainer != null)
            {
                CustomFieldsContainer.Children.Clear();
            }
            
            // Reset title
            Title = "Add New Job";
            
            // Show job type picker and ensure it's always available for new jobs
            if (JobTypeLabel != null) JobTypeLabel.IsVisible = true;
            if (JobTypePicker != null) 
            {
                JobTypePicker.IsVisible = true;
                JobTypePicker.SelectedIndex = -1; // Clear selection
                // Reload job types to get any new ones
                await LoadJobTypesAsync();
            }
            
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await InitializeJobTypeServiceAsync();
            
            // Always reload job types to get any newly pulled items
            await LoadJobTypesAsync();
            
            if (!_isEditing)
            {
                // For new jobs, ensure everything is cleared
                // Only load pre-selected job type if coming from JobTypeDetailPage
                if (_preSelectedJobTypeId.HasValue)
                {
                    // Clear form first
                    await ClearFormAsync();
                    await LoadJobTypesAsync(); // Reload after clearing
                    
                    // Then load the pre-selected job type
                    await LoadJobTypeAsync(_preSelectedJobTypeId.Value);
                    // Select it in the picker
                    if (JobTypePicker != null && _jobTypeService != null)
                    {
                        var jobTypes = await _jobTypeService.GetJobTypesAsync();
                        var index = jobTypes.FindIndex(jt => jt.Id == _preSelectedJobTypeId.Value);
                        if (index >= 0)
                        {
                            JobTypePicker.SelectedIndex = index;
                        }
                    }
                    _preSelectedJobTypeId = null; // Clear it after loading
                }
                else
                {
                    // Fresh entry - clear everything (job types already loaded above)
                    await ClearFormAsync();
                }
            }
            else
            {
                // Load job type for editing
                if (_job.JobTypeId > 0)
                {
                    await LoadJobTypeAsync(_job.JobTypeId);
                }
            }
            SetupStatusPicker();
        }

        private async Task InitializeJobTypeServiceAsync()
        {
            if (_jobTypeService == null)
            {
                try
                {
                    var authService = new FirebaseAuthService();
                    var currentUser = await authService.GetCurrentUserAsync();
                    var userId = currentUser?.Id;
                    _jobTypeService = await JobTypeService.CreateAsync(userId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error initializing JobTypeService: {ex.Message}");
                    _jobTypeService = await JobTypeService.CreateAsync(null);
                }
            }
        }

        private async Task LoadJobTypesAsync()
        {
            try
            {
                if (_jobTypeService == null) return;
                
                var jobTypes = await _jobTypeService.GetJobTypesAsync();
                var jobTypeNames = jobTypes.Select(jt => jt.Name).ToList();
                
                if (JobTypePicker != null)
                {
                    JobTypePicker.ItemsSource = jobTypeNames;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading job types: {ex.Message}");
            }
        }

        private async void OnJobTypeSelected(object sender, EventArgs e)
        {
            if (JobTypePicker == null || JobTypePicker.SelectedIndex < 0 || _jobTypeService == null) 
            {
                // If selection was cleared, hide all fields
                if (JobTypePicker != null && JobTypePicker.SelectedIndex < 0)
                {
                    _selectedJobType = null;
                    _job.JobTypeId = 0;
                    // Reset description placeholder
                    if (DescriptionEditor != null)
                    {
                        DescriptionEditor.Placeholder = "Enter job description";
                    }
                    UpdateFieldVisibility();
                }
                return;
            }
            
            try
            {
                // Clear previous custom field values when changing job type
                _customFieldValues.Clear();
                if (CustomFieldsContainer != null)
                {
                    CustomFieldsContainer.Children.Clear();
                }
                
                var jobTypes = await _jobTypeService.GetJobTypesAsync();
                if (JobTypePicker.SelectedIndex < jobTypes.Count)
                {
                    var selectedJobType = jobTypes[JobTypePicker.SelectedIndex];
                    await LoadJobTypeAsync(selectedJobType.Id);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load job type: {ex.Message}", "OK");
            }
        }

        private async Task LoadJobTypeAsync(int jobTypeId)
        {
            try
            {
                if (_jobTypeService == null) return;
                
                _selectedJobType = await _jobTypeService.GetJobTypeAsync(jobTypeId);
                if (_selectedJobType != null)
                {
                    _job.JobTypeId = jobTypeId;
                    
                    // Deserialize custom fields if needed
                    if (!string.IsNullOrEmpty(_selectedJobType.CustomFields) && 
                        (_selectedJobType.CustomFieldsList == null || _selectedJobType.CustomFieldsList.Count == 0))
                    {
                        try
                        {
                            var customFields = JsonSerializer.Deserialize<List<CustomField>>(_selectedJobType.CustomFields);
                            if (customFields != null)
                            {
                                _selectedJobType.CustomFieldsList = new ObservableCollection<CustomField>(customFields);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error deserializing custom fields: {ex.Message}");
                        }
                    }
                    
                    // Update description placeholder with job type description
                    if (DescriptionEditor != null && !string.IsNullOrEmpty(_selectedJobType.Description))
                    {
                        DescriptionEditor.Placeholder = _selectedJobType.Description;
                    }
                    else if (DescriptionEditor != null)
                    {
                        DescriptionEditor.Placeholder = "Enter job description";
                    }
                    
                    UpdateFieldVisibility();
                    UpdateStatusPicker();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading job type: {ex.Message}");
            }
        }

        private void UpdateFieldVisibility()
        {
            // If editing and no job type, show all fields (backward compatibility)
            if (_isEditing && _selectedJobType == null)
            {
                if (ClientInfoSection != null) ClientInfoSection.IsVisible = true;
                if (PricingSection != null) PricingSection.IsVisible = true;
                if (StatusSection != null) StatusSection.IsVisible = true;
                if (DueDateSection != null) DueDateSection.IsVisible = true;
                if (LocationSection != null) LocationSection.IsVisible = true;
                if (PhotosSection != null) PhotosSection.IsVisible = true;
                if (NotesSection != null) NotesSection.IsVisible = true;
                if (UrgentSection != null) UrgentSection.IsVisible = true;
                if (CustomFieldsSection != null) CustomFieldsSection.IsVisible = false;
                return;
            }

            if (_selectedJobType == null) return;

            if (ClientInfoSection != null)
                ClientInfoSection.IsVisible = _selectedJobType.HasClientInfo;
            
            if (PricingSection != null)
                PricingSection.IsVisible = _selectedJobType.HasPricing;
            
            if (StatusSection != null)
                StatusSection.IsVisible = _selectedJobType.HasStatus;
            
            if (DueDateSection != null)
                DueDateSection.IsVisible = _selectedJobType.HasDueDate;
            
            if (LocationSection != null)
                LocationSection.IsVisible = _selectedJobType.HasLocation;
            
            if (PhotosSection != null)
                PhotosSection.IsVisible = _selectedJobType.HasPhotos;
            
            if (NotesSection != null)
                NotesSection.IsVisible = _selectedJobType.HasNotes;
            
            if (UrgentSection != null)
                UrgentSection.IsVisible = _selectedJobType.HasUrgentFlag;

            // Update custom fields
            UpdateCustomFields();
        }

        private void UpdateCustomFields()
        {
            if (CustomFieldsContainer == null || _selectedJobType == null) return;

            // Clear existing custom fields
            CustomFieldsContainer.Children.Clear();
            _customFieldViews.Clear();

            // Load custom fields from job type
            if (string.IsNullOrEmpty(_selectedJobType.CustomFields))
            {
                if (CustomFieldsSection != null) CustomFieldsSection.IsVisible = false;
                return;
            }

            try
            {
                var customFields = JsonSerializer.Deserialize<List<CustomField>>(_selectedJobType.CustomFields);
                if (customFields == null || customFields.Count == 0)
                {
                    if (CustomFieldsSection != null) CustomFieldsSection.IsVisible = false;
                    return;
                }

                if (CustomFieldsSection != null) CustomFieldsSection.IsVisible = true;

                // Load existing values if editing
                if (_isEditing && !string.IsNullOrEmpty(_job.CustomFieldValues))
                {
                    try
                    {
                        _customFieldValues = JsonSerializer.Deserialize<Dictionary<string, string>>(_job.CustomFieldValues) 
                            ?? new Dictionary<string, string>();
                    }
                    catch
                    {
                        _customFieldValues = new Dictionary<string, string>();
                    }
                }

                // Create UI for each custom field
                foreach (var field in customFields)
                {
                    var fieldContainer = new StackLayout { Spacing = 5, Margin = new Thickness(0, 5, 0, 0) };
                    var label = new Label 
                    { 
                        Text = field.Name + (field.Required ? " *" : ""),
                        FontSize = 16,
                        FontAttributes = FontAttributes.Bold
                    };
                    fieldContainer.Children.Add(label);

                    View inputControl = null;
                    string fieldKey = field.Name;

                    switch (field.Type.ToLower())
                    {
                        case "text":
                            var textEntry = new Entry { Placeholder = $"Enter {field.Name}" };
                            if (_customFieldValues.ContainsKey(fieldKey))
                                textEntry.Text = _customFieldValues[fieldKey];
                            textEntry.TextChanged += (s, e) => 
                            {
                                if (string.IsNullOrEmpty(e.NewTextValue))
                                    _customFieldValues.Remove(fieldKey);
                                else
                                    _customFieldValues[fieldKey] = e.NewTextValue;
                            };
                            inputControl = textEntry;
                            break;

                        case "number":
                            var numberEntry = new Entry { Placeholder = $"Enter {field.Name}", Keyboard = Keyboard.Numeric };
                            if (_customFieldValues.ContainsKey(fieldKey))
                                numberEntry.Text = _customFieldValues[fieldKey];
                            numberEntry.TextChanged += (s, e) => 
                            {
                                if (string.IsNullOrEmpty(e.NewTextValue))
                                    _customFieldValues.Remove(fieldKey);
                                else
                                    _customFieldValues[fieldKey] = e.NewTextValue;
                            };
                            inputControl = numberEntry;
                            break;

                        case "date":
                            var datePicker = new DatePicker();
                            if (_customFieldValues.ContainsKey(fieldKey) && DateTime.TryParse(_customFieldValues[fieldKey], out DateTime dateValue))
                                datePicker.Date = dateValue;
                            datePicker.DateSelected += (s, e) => 
                            {
                                _customFieldValues[fieldKey] = e.NewDate.ToString("yyyy-MM-dd");
                            };
                            inputControl = datePicker;
                            break;

                        case "boolean":
                            var checkBox = new CheckBox();
                            var checkBoxContainer = new StackLayout { Orientation = StackOrientation.Horizontal };
                            if (_customFieldValues.ContainsKey(fieldKey) && bool.TryParse(_customFieldValues[fieldKey], out bool boolValue))
                                checkBox.IsChecked = boolValue;
                            checkBox.CheckedChanged += (s, e) => 
                            {
                                _customFieldValues[fieldKey] = e.Value.ToString();
                            };
                            checkBoxContainer.Children.Add(checkBox);
                            checkBoxContainer.Children.Add(new Label { Text = field.Name, VerticalOptions = LayoutOptions.Center, Margin = new Thickness(10, 0, 0, 0) });
                            inputControl = checkBoxContainer;
                            break;

                        default:
                            // Default to text
                            var defaultEntry = new Entry { Placeholder = $"Enter {field.Name}" };
                            if (_customFieldValues.ContainsKey(fieldKey))
                                defaultEntry.Text = _customFieldValues[fieldKey];
                            defaultEntry.TextChanged += (s, e) => 
                            {
                                if (string.IsNullOrEmpty(e.NewTextValue))
                                    _customFieldValues.Remove(fieldKey);
                                else
                                    _customFieldValues[fieldKey] = e.NewTextValue;
                            };
                            inputControl = defaultEntry;
                            break;
                    }

                    if (inputControl != null)
                    {
                        fieldContainer.Children.Add(inputControl);
                        _customFieldViews[fieldKey] = inputControl;
                        CustomFieldsContainer.Children.Add(fieldContainer);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading custom fields: {ex.Message}");
                if (CustomFieldsSection != null) CustomFieldsSection.IsVisible = false;
            }
        }

        private void UpdateStatusPicker()
        {
            if (StatusPicker == null || _selectedJobType == null) return;
            
            var statusOptions = _selectedJobType.StatusList;
            if (statusOptions != null && statusOptions.Count > 0)
            {
                StatusPicker.ItemsSource = statusOptions;
                if (statusOptions.Count > 0)
                {
                    StatusPicker.SelectedItem = _job.Status ?? statusOptions[0];
                }
            }
            else
            {
                StatusPicker.ItemsSource = new List<string> { "Pending", "In Progress", "Completed", "Cancelled" };
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
                    BindingContext = this; // Changed to bind to this page for Photos collection
                    
                    // Hide job type picker for editing
                    if (JobTypeLabel != null) JobTypeLabel.IsVisible = false;
                    if (JobTypePicker != null) JobTypePicker.IsVisible = false;
                    
                    // Load job type if it exists
                    if (_job.JobTypeId > 0)
                    {
                        await LoadJobTypeAsync(_job.JobTypeId);
                    }
                    
                    SetupStatusPicker();
                    
                    // Load existing photos
                    Photos.Clear();
                    foreach (var photo in _job.PhotoList)
                    {
                        Photos.Add(photo);
                    }

                    // Load custom field values
                    if (!string.IsNullOrEmpty(_job.CustomFieldValues))
                    {
                        try
                        {
                            _customFieldValues = JsonSerializer.Deserialize<Dictionary<string, string>>(_job.CustomFieldValues) 
                                ?? new Dictionary<string, string>();
                        }
                        catch
                        {
                            _customFieldValues = new Dictionary<string, string>();
                        }
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
            if (StatusPicker == null) return;
            
            if (_selectedJobType != null)
            {
                UpdateStatusPicker();
            }
            else
        {
            StatusPicker.ItemsSource = new List<string> { "Pending", "In Progress", "Completed", "Cancelled" };
            StatusPicker.SelectedItem = _job.Status;
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (await ValidateForm())
            {
                try
                {
                    // Update status from picker
                    _job.Status = StatusPicker.SelectedItem?.ToString() ?? "Pending";
                    
                    // Update due date
                    _job.DueDate = DueDatePicker.Date;
                    
                    // Save photos
                    _job.PhotoList = Photos.ToList();

                    // Save custom field values
                    if (_customFieldValues.Count > 0)
                    {
                        _job.CustomFieldValues = JsonSerializer.Serialize(_customFieldValues);
                    }
                    else
                    {
                        _job.CustomFieldValues = string.Empty;
                    }

                    await _photoJobService.SaveJobAsync(_job);
                    
                    string message = _isEditing ? "Job updated successfully!" : "Job added successfully!";
                    await DisplayAlert("Success", message, "OK");
                    
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
                            await DisplayAlert("Navigation Error", "Job saved but unable to navigate. Please use the menu to go back.", "OK");
                            }
                        }
                    else
                    {
                        // When creating a new job, clear the form completely to allow creating another job
                        // Don't restore job type - let user select fresh for each new job
                        await ClearFormAsync();
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to save job: {ex.Message}", "OK");
                }
            }
        }

        private async Task<bool> ValidateForm()
        {
            if (!_isEditing && (_job.JobTypeId == 0 || JobTypePicker?.SelectedIndex < 0))
            {
                await DisplayAlert("Validation Error", "Please select a job type.", "OK");
                JobTypePicker?.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(_job.Title))
            {
                await DisplayAlert("Validation Error", "Please enter a job title.", "OK");
                TitleEntry.Focus();
                return false;
            }

            if (_selectedJobType?.HasClientInfo == true && string.IsNullOrWhiteSpace(_job.ClientName))
            {
                await DisplayAlert("Validation Error", "Please enter a client name.", "OK");
                ClientNameEntry.Focus();
                return false;
            }

            if (_selectedJobType?.HasPricing == true && _job.Price <= 0)
            {
                await DisplayAlert("Validation Error", "Please enter a valid price greater than 0.", "OK");
                PriceEntry.Focus();
                return false;
            }

            return true;
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
    }
} 