using PhotoJobApp.Models;
using PhotoJobApp.Services;

namespace PhotoJobApp
{
    [QueryProperty(nameof(JobTypeId), "JobType")]
    public partial class JobTypeDetailPage : ContentPage
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

        public JobTypeDetailPage()
        {
            InitializeComponent();
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
                    BindingContext = _jobType;
                    
                    // Apply theme from job type
                    ApplyThemeFromJobType();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load job type: {ex.Message}", "OK");
            }
        }

        private void ApplyThemeFromJobType()
        {
            try
            {
                if (_jobType != null)
                {
                    // Update theme colors from the job type
                    ThemeService.Instance.UpdateThemeFromJobType(_jobType);
                    
                    // Apply the updated theme with custom background color
                    var jobTypeColor = Microsoft.Maui.Graphics.Color.FromArgb(_jobType.Color);
                    var darkerBackground = ThemeService.Instance.GetLighterColor(jobTypeColor, 0.7f);
                    ThemeService.Instance.ApplyThemeToPage(this, darkerBackground);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying theme from job type: {ex.Message}");
            }
        }

        private async void OnEditClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync($"AddEditJobTypePage?JobType={_jobType.Id}");
        }

        private async void OnCreateJobClicked(object sender, EventArgs e)
        {
            // Navigate to AddEditJobPage with the job type pre-selected
            await Shell.Current.GoToAsync($"AddEditJobPage?JobTypeId={_jobType.Id}");
        }

        private async void OnPushToCloudClicked(object sender, EventArgs e)
        {
            try
            {
                // Get current user
                var authService = new FirebaseAuthService();
                var currentUser = await authService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    await DisplayAlert("Error", "You must be signed in to push to cloud", "OK");
                    return;
                }

                // Initialize cloud service
                var cloudService = new CloudJobTypeService(currentUser.Id);
                
                // Push the job type to cloud
                await cloudService.SaveJobTypeAsync(_jobType);
                
                await DisplayAlert("Success", "Job type pushed to cloud successfully", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to push job type to cloud: {ex.Message}", "OK");
            }
        }


        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Confirm Delete", 
                $"Are you sure you want to delete '{_jobType.Name}'? This action cannot be undone.", "Yes", "No");
            
            if (confirm)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Deleting job type: {_jobType.Name} (ID: {_jobType.Id})");
                    Console.WriteLine($"Deleting job type: {_jobType.Name} (ID: {_jobType.Id})");
                    
                    await _jobTypeService.DeleteJobTypeAsync(_jobType);
                    await DisplayAlert("Success", "Job type deleted successfully", "OK");
                    
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
                                await DisplayAlert("Navigation Error", "Job type deleted but unable to go back. Please restart the app.", "OK");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deleting job type: {ex.Message}");
                    Console.WriteLine($"Error deleting job type: {ex.Message}");
                    await DisplayAlert("Error", $"Failed to delete job type: {ex.Message}", "OK");
                }
            }
        }
    }
} 