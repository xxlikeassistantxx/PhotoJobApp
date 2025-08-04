using PhotoJobApp.Models;
using PhotoJobApp.Services;

namespace PhotoJobApp
{
    [QueryProperty(nameof(JobId), "Job")]
    public partial class JobDetailPage : ContentPage
    {
        private readonly PhotoJobService _photoJobService;
        private PhotoJob _job;

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
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load job: {ex.Message}", "OK");
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
                var currentUser = await authService.GetCurrentUserAsync();
                var userId = currentUser?.Id;
                
                if (!string.IsNullOrEmpty(userId))
                {
                    var cloudJobService = new CloudJobService(userId);
                    var success = await cloudJobService.SaveJobAsync(_job);
                    
                    if (success)
                    {
                        await DisplayAlert("Success", $"'{_job.Title}' has been pushed to the cloud successfully.", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Error", "Failed to push job to cloud. Please check your internet connection.", "OK");
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
            await Shell.Current.GoToAsync($"AddEditJobPage?Job={_job.Id}");
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