using PhotoJobApp.Models;
using PhotoJobApp.Services;
using System.Text.Json;

namespace PhotoJobApp
{
    [QueryProperty(nameof(JobId), "Job")]
    public partial class JobDetailPage : ContentPage
    {
        private readonly PhotoJobService _photoJobService;
        private JobTypeService _jobTypeService;
        private PhotoJob _job;
        private JobType _jobType;

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

        public JobDetailPage()
        {
            InitializeComponent();
            _photoJobService = new PhotoJobService();
        }

        private async void LoadJobAsync(int jobId)
        {
            try
            {
                var job = await _photoJobService.GetJobAsync(jobId);
                if (job != null)
                {
                    _job = job;
                    BindingContext = _job;
                    
                    // Initialize job type service and load job type
                    await InitializeJobTypeServiceAsync();
                    
                    if (_job.JobTypeId > 0 && _jobTypeService != null)
                    {
                        _jobType = await _jobTypeService.GetJobTypeAsync(_job.JobTypeId);
                        if (_jobType != null)
                        {
                            UpdateFieldVisibility();
                            UpdateCustomFields();
                        }
                    }
                    else
                    {
                        // If no job type, show all fields for backward compatibility
                        ShowAllFields();
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load job: {ex.Message}", "OK");
            }
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

        private void ShowAllFields()
        {
            if (ClientInfoSection != null) ClientInfoSection.IsVisible = true;
            if (PricingSection != null) PricingSection.IsVisible = true;
            if (StatusSection != null) StatusSection.IsVisible = true;
            if (DueDateSection != null) DueDateSection.IsVisible = true;
            if (LocationSection != null) LocationSection.IsVisible = true;
            if (PhotosSection != null) PhotosSection.IsVisible = _job?.PhotoList?.Count > 0;
            if (NotesSection != null) NotesSection.IsVisible = true;
            if (UrgentSection != null) UrgentSection.IsVisible = true;
            // Description is always shown if it has content
            if (DescriptionSection != null) DescriptionSection.IsVisible = !string.IsNullOrEmpty(_job?.Description);
            if (CustomFieldsSection != null) CustomFieldsSection.IsVisible = false;
        }

        private void UpdateFieldVisibility()
        {
            if (_jobType == null)
            {
                ShowAllFields();
                return;
            }

            // Show/hide sections based on job type features
            if (ClientInfoSection != null)
                ClientInfoSection.IsVisible = _jobType.HasClientInfo;
            
            if (PricingSection != null)
                PricingSection.IsVisible = _jobType.HasPricing;
            
            if (StatusSection != null)
                StatusSection.IsVisible = _jobType.HasStatus;
            
            if (DueDateSection != null)
                DueDateSection.IsVisible = _jobType.HasDueDate;
            
            if (LocationSection != null)
                LocationSection.IsVisible = _jobType.HasLocation;
            
            if (PhotosSection != null)
                PhotosSection.IsVisible = _jobType.HasPhotos && (_job?.PhotoList?.Count > 0);
            
            if (NotesSection != null)
                NotesSection.IsVisible = _jobType.HasNotes;
            
            if (UrgentSection != null)
                UrgentSection.IsVisible = _jobType.HasUrgentFlag;
            
            // Description is always shown if it has content
            if (DescriptionSection != null)
                DescriptionSection.IsVisible = !string.IsNullOrEmpty(_job?.Description);
        }

        private void UpdateCustomFields()
        {
            if (CustomFieldsContainer == null || _jobType == null) return;

            // Clear existing custom fields
            CustomFieldsContainer.Children.Clear();

            // Load custom fields from job type
            if (string.IsNullOrEmpty(_jobType.CustomFields))
            {
                if (CustomFieldsSection != null) CustomFieldsSection.IsVisible = false;
                return;
            }

            try
            {
                var customFields = JsonSerializer.Deserialize<List<CustomField>>(_jobType.CustomFields);
                if (customFields == null || customFields.Count == 0)
                {
                    if (CustomFieldsSection != null) CustomFieldsSection.IsVisible = false;
                    return;
                }

                // Load existing values from job
                Dictionary<string, string> customFieldValues = new();
                if (!string.IsNullOrEmpty(_job.CustomFieldValues))
                {
                    try
                    {
                        customFieldValues = JsonSerializer.Deserialize<Dictionary<string, string>>(_job.CustomFieldValues) 
                            ?? new Dictionary<string, string>();
                    }
                    catch
                    {
                        customFieldValues = new Dictionary<string, string>();
                    }
                }

                if (CustomFieldsSection != null) CustomFieldsSection.IsVisible = true;

                // Create display for each custom field
                foreach (var field in customFields)
                {
                    var fieldContainer = new StackLayout
                    {
                        Spacing = 5,
                        Margin = new Thickness(0, 5, 0, 0)
                    };

                    var label = new Label
                    {
                        Text = field.Name,
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 14
                    };
                    fieldContainer.Children.Add(label);

                    // Get the value for this field
                    var value = customFieldValues.ContainsKey(field.Name) 
                        ? customFieldValues[field.Name] 
                        : field.DefaultValue ?? "";

                    Label valueLabel = new Label
                    {
                        Text = !string.IsNullOrEmpty(value) ? value : "(Not set)",
                        FontSize = 14,
                        TextColor = !string.IsNullOrEmpty(value) ? Colors.Black : Colors.Gray,
                        Margin = new Thickness(10, 0, 0, 0)
                    };
                    fieldContainer.Children.Add(valueLabel);

                    CustomFieldsContainer.Children.Add(fieldContainer);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading custom fields: {ex.Message}");
                if (CustomFieldsSection != null) CustomFieldsSection.IsVisible = false;
            }
        }

        private async void OnPushToCloudClicked(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Push to cloud clicked for job: {_job.Title}");
                Console.WriteLine($"Push to cloud clicked for job: {_job.Title}");
                
                // Get current user for cloud sync
                var authService = new FirebaseAuthService();
                
                // Always check and refresh auth state to ensure we have a valid token
                var (isAuthenticated, currentUser) = await authService.CheckAuthStateAsync();
                
                if (!isAuthenticated || currentUser == null)
                {
                    // Try to get from local storage as fallback
                    currentUser = await authService.GetCurrentUserAsync();
                }
                
                // If still no user or no token, try to refresh
                if (currentUser != null && string.IsNullOrEmpty(currentUser.IdToken) && !string.IsNullOrEmpty(currentUser.RefreshToken))
                {
                    currentUser = await authService.RefreshIdTokenAsync(currentUser.RefreshToken);
                }
                
                var userId = currentUser?.Id;
                
                if (!string.IsNullOrEmpty(userId))
                {
                    // Get the ID token for proper Firebase authentication
                    var idToken = currentUser?.IdToken;
                    
                    System.Diagnostics.Debug.WriteLine($"JobDetailPage: UserId: {userId}, HasIdToken: {!string.IsNullOrEmpty(idToken)}, TokenLength: {idToken?.Length ?? 0}");
                    
                    if (string.IsNullOrEmpty(idToken))
                    {
                        await DisplayAlert("Authentication Error", 
                            "No valid authentication token found. Please sign out and sign in again.", 
                            "OK");
                        return;
                    }
                    
                    var cloudJobService = new CloudJobService(userId, idToken);
                    
                    // Save with sharing enabled (will share with linked accounts)
                    var (success, errorMessage) = await cloudJobService.SaveJobAsync(_job, shareWithLinkedAccounts: true);
                    
                    if (success)
                    {
                        // Update local job with cloud ID if needed
                        if (string.IsNullOrEmpty(_job.CloudId) && _job.Id > 0)
                        {
                            _job.CloudId = _job.Id.ToString();
                            await _photoJobService.SaveJobAsync(_job);
                        }
                        
                        await DisplayAlert("Success", 
                            $"'{_job.Title}' has been pushed to the cloud successfully and shared with linked accounts.", 
                            "OK");
                    }
                    else
                    {
                        var errorMsg = !string.IsNullOrEmpty(errorMessage) 
                            ? $"Failed to push job to cloud: {errorMessage}" 
                            : "Failed to push job to cloud. Please check your internet connection and Firebase configuration.";
                        await DisplayAlert("Error", errorMsg, "OK");
                    }
                }
                else
                {
                    await DisplayAlert("Error", "Please sign in to use cloud features.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Push to cloud error: {ex.Message}");
                Console.WriteLine($"Push to cloud error: {ex.Message}");
                await DisplayAlert("Error", $"Failed to push to cloud: {ex.Message}", "OK");
            }
        }

        private async void OnEditClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync($"///AddEditJobPage?Job={_job.Id}");
        }

        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Confirm Delete", 
                $"Are you sure you want to delete '{_job.Title}'?", "Yes", "No");
            
            if (confirm)
            {
                try
                {
                    await _photoJobService.DeleteJobAsync(_job);
                    await DisplayAlert("Success", "Job deleted successfully", "OK");
                    
                    try
                    {
                        // Try Shell navigation first
                    await Shell.Current.GoToAsync("..");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Shell navigation failed after delete: {ex.Message}");
                        Console.WriteLine($"Shell navigation failed after delete: {ex.Message}");
                        
                        // Fallback: Create a new AppShell and set it as the window page
                        try
                        {
                            if (Application.Current.Windows.Count > 0)
                            {
                                var authService = new FirebaseAuthService();
                                var appShell = new AppShell(authService);
                                Application.Current.Windows[0].Page = appShell;
                                
                                System.Diagnostics.Debug.WriteLine("Delete navigation completed via fallback");
                                Console.WriteLine("Delete navigation completed via fallback");
                            }
                        }
                        catch (Exception fallbackEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Delete fallback navigation failed: {fallbackEx.Message}");
                            Console.WriteLine($"Delete fallback navigation failed: {fallbackEx.Message}");
                            
                            // Final fallback: Go back to MainPage
                            try
                            {
                                if (Application.Current.Windows.Count > 0)
                                {
                                    var mainPage = new MainPage();
                                    Application.Current.Windows[0].Page = mainPage;
                                    
                                    System.Diagnostics.Debug.WriteLine("Delete navigation completed via MainPage fallback");
                                    Console.WriteLine("Delete navigation completed via MainPage fallback");
                                }
                            }
                            catch (Exception finalEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Delete final fallback navigation failed: {finalEx.Message}");
                                Console.WriteLine($"Delete final fallback navigation failed: {finalEx.Message}");
                                await DisplayAlert("Navigation Error", "Job deleted but unable to go back. Please restart the app.", "OK");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to delete job: {ex.Message}", "OK");
                }
            }
        }

        private async void OnViewPhotosClicked(object sender, EventArgs e)
        {
            if (_job?.PhotoList?.Count > 0)
            {
                try
                {
                    var photoList = new System.Collections.ObjectModel.ObservableCollection<string>(_job.PhotoList);
                    var parameters = new Dictionary<string, object>
                    {
                        { "PhotoList", photoList },
                        { "InitialIndex", 0 }
                    };
                    
                    await Shell.Current.GoToAsync("PhotoGalleryPage", parameters);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to open photo gallery: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("No Photos", "This job doesn't have any photos to view.", "OK");
            }
        }

        private async void OnPhotoTapped(object sender, EventArgs e)
        {
            if (_job?.PhotoList?.Count > 0)
            {
                try
                {
                    // Get the tapped photo index from the gesture recognizer
                    var grid = sender as Grid;
                    var image = grid?.Children?.FirstOrDefault() as Image;
                    
                    if (image?.Source != null)
                    {
                        var photoPath = image.Source.ToString();
                        var photoIndex = _job.PhotoList.IndexOf(photoPath);
                        
                        if (photoIndex >= 0)
                        {
                            var photoList = new System.Collections.ObjectModel.ObservableCollection<string>(_job.PhotoList);
                            var parameters = new Dictionary<string, object>
                            {
                                { "PhotoList", photoList },
                                { "InitialIndex", photoIndex }
                            };
                            
                            await Shell.Current.GoToAsync("PhotoGalleryPage", parameters);
                        }
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to open photo: {ex.Message}", "OK");
                }
            }
        }
    }
} 