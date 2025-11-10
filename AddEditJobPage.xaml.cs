using PhotoJobApp.Models;
using PhotoJobApp.Services;
using System.Collections.ObjectModel;

namespace PhotoJobApp
{
    [QueryProperty(nameof(JobId), "Job")]
    public partial class AddEditJobPage : ContentPage
    {
        private readonly PhotoJobService _photoJobService;
        private PhotoJob _job;
        private bool _isEditing;
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

        public AddEditJobPage()
        {
            InitializeComponent();
            _photoJobService = new PhotoJobService();
            _job = new PhotoJob();
            _isEditing = false;
            Title = "Add New Job";
            BindingContext = this; // Changed to bind to this page for Photos collection
            SetupStatusPicker();
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
                    SetupStatusPicker();
                    
                    // Load existing photos
                    Photos.Clear();
                    foreach (var photo in _job.PhotoList)
                    {
                        Photos.Add(photo);
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
            StatusPicker.ItemsSource = new List<string> { "Pending", "In Progress", "Completed", "Cancelled" };
            StatusPicker.SelectedItem = _job.Status;
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (ValidateForm())
            {
                try
                {
                    // Update status from picker
                    _job.Status = StatusPicker.SelectedItem?.ToString() ?? "Pending";
                    
                    // Update due date
                    _job.DueDate = DueDatePicker.Date;
                    
                    // Save photos
                    _job.PhotoList = Photos.ToList();

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

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(_job.Title))
            {
                DisplayAlert("Validation Error", "Please enter a job title.", "OK");
                TitleEntry.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(_job.ClientName))
            {
                DisplayAlert("Validation Error", "Please enter a client name.", "OK");
                ClientNameEntry.Focus();
                return false;
            }

            if (_job.Price <= 0)
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
    }
} 