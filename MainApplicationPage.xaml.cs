using System.Collections.ObjectModel;
using PhotoJobApp.Models;
using PhotoJobApp.Services;

namespace PhotoJobApp;

public partial class MainApplicationPage : ContentPage
{
    private readonly PhotoJobService _photoJobService = null!;
    private readonly FirebaseAuthService _authService = null!;
    public ObservableCollection<PhotoJob> Jobs { get; set; } = null!;

    public MainApplicationPage(FirebaseAuthService authService)
    {
        System.Diagnostics.Debug.WriteLine("=== MainApplicationPage constructor START ===");
        Console.WriteLine("=== MainApplicationPage constructor START ===");
        
        try
        {
            System.Diagnostics.Debug.WriteLine("About to call InitializeComponent...");
            Console.WriteLine("About to call InitializeComponent...");
            
            InitializeComponent();
            
            System.Diagnostics.Debug.WriteLine("InitializeComponent completed successfully");
            Console.WriteLine("InitializeComponent completed successfully");
            
            System.Diagnostics.Debug.WriteLine("About to initialize _photoJobService...");
            Console.WriteLine("About to initialize _photoJobService...");
            
            _photoJobService = new PhotoJobService();
            
            System.Diagnostics.Debug.WriteLine("_photoJobService initialized successfully");
            Console.WriteLine("_photoJobService initialized successfully");
            
            System.Diagnostics.Debug.WriteLine("About to set _authService...");
            Console.WriteLine("About to set _authService...");
            
            _authService = authService;
            
            System.Diagnostics.Debug.WriteLine("_authService set successfully");
            Console.WriteLine("_authService set successfully");
            
            System.Diagnostics.Debug.WriteLine("About to initialize Jobs collection...");
            Console.WriteLine("About to initialize Jobs collection...");
            
            Jobs = new ObservableCollection<PhotoJob>();
            
            System.Diagnostics.Debug.WriteLine("Jobs collection initialized successfully");
            Console.WriteLine("Jobs collection initialized successfully");
            
            System.Diagnostics.Debug.WriteLine("About to set BindingContext...");
            Console.WriteLine("About to set BindingContext...");
            
            BindingContext = this;
            
            System.Diagnostics.Debug.WriteLine("BindingContext set successfully");
            Console.WriteLine("BindingContext set successfully");
            
            System.Diagnostics.Debug.WriteLine("About to call LoadJobsAsync...");
            Console.WriteLine("About to call LoadJobsAsync...");
            
            LoadJobsAsync();
            
            System.Diagnostics.Debug.WriteLine("LoadJobsAsync called successfully");
            Console.WriteLine("LoadJobsAsync called successfully");
            
            System.Diagnostics.Debug.WriteLine("=== MainApplicationPage constructor COMPLETED SUCCESSFULLY ===");
            Console.WriteLine("=== MainApplicationPage constructor COMPLETED SUCCESSFULLY ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"=== ERROR in MainApplicationPage constructor ===");
            Console.WriteLine($"=== ERROR in MainApplicationPage constructor ===");
            System.Diagnostics.Debug.WriteLine($"Error message: {ex.Message}");
            Console.WriteLine($"Error message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Error type: {ex.GetType().Name}");
            Console.WriteLine($"Error type: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    protected override void OnAppearing()
    {
        System.Diagnostics.Debug.WriteLine("MainApplicationPage OnAppearing called");
        Console.WriteLine("MainApplicationPage OnAppearing called");
        
        try
        {
            base.OnAppearing();
            LoadJobsAsync();
            
            System.Diagnostics.Debug.WriteLine("MainApplicationPage OnAppearing completed");
            Console.WriteLine("MainApplicationPage OnAppearing completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnAppearing: {ex.Message}");
            Console.WriteLine($"Error in OnAppearing: {ex.Message}");
        }
    }

    private async void LoadJobsAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("LoadJobsAsync called");
            Console.WriteLine("LoadJobsAsync called");
            
            if (_photoJobService == null)
            {
                System.Diagnostics.Debug.WriteLine("_photoJobService is null, skipping database load");
                Console.WriteLine("_photoJobService is null, skipping database load");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine("Loading jobs from database...");
            Console.WriteLine("Loading jobs from database...");
            
            var jobs = await _photoJobService.GetJobsAsync();
            System.Diagnostics.Debug.WriteLine($"Loaded {jobs.Count} jobs from database");
            Console.WriteLine($"Loaded {jobs.Count} jobs from database");
            
            Jobs.Clear();
            foreach (var job in jobs)
            {
                System.Diagnostics.Debug.WriteLine($"Adding job: {job.Title} (Created: {job.CreatedDate})");
                Console.WriteLine($"Adding job: {job.Title} (Created: {job.CreatedDate})");
                Jobs.Add(job);
            }
            
            System.Diagnostics.Debug.WriteLine($"Total jobs in collection: {Jobs.Count}");
            Console.WriteLine($"Total jobs in collection: {Jobs.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading jobs: {ex.Message}");
            Console.WriteLine($"Error loading jobs: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            await DisplayAlert("Error", $"Failed to load jobs: {ex.Message}", "OK");
        }
    }

    private async void OnAddJobClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AddEditJobPage");
    }

    private async void OnJobTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is PhotoJob job)
        {
            await Shell.Current.GoToAsync($"JobDetailPage?Job={job.Id}");
        }
    }

    private async void OnJobLongPressed(object sender, TappedEventArgs e)
    {
        if (e.Parameter is PhotoJob job)
        {
            var action = await DisplayActionSheet("Job Options", "Cancel", null, "Edit", "Delete", "Mark Complete");
            
            switch (action)
            {
                case "Edit":
                    await Shell.Current.GoToAsync($"AddEditJobPage?Job={job.Id}");
                    break;
                case "Delete":
                    await DeleteJobAsync(job);
                    break;
                case "Mark Complete":
                    await MarkJobCompleteAsync(job);
                    break;
            }
        }
    }

    private async Task DeleteJobAsync(PhotoJob job)
    {
        var confirm = await DisplayAlert("Confirm Delete", 
            $"Are you sure you want to delete '{job.Title}'?", "Yes", "No");
        
        if (confirm)
        {
            try
            {
                await _photoJobService.DeleteJobAsync(job);
                Jobs.Remove(job);
                await DisplayAlert("Success", "Job deleted successfully", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to delete job: {ex.Message}", "OK");
            }
        }
    }

            private async Task MarkJobCompleteAsync(PhotoJob job)
        {
            try
            {
                job.Status = "Completed";
                job.CompletedDate = DateTime.Now;
                await _photoJobService.SaveJobAsync(job);
                
                // Refresh the job in the collection
                var index = Jobs.IndexOf(job);
                if (index >= 0)
                {
                    Jobs[index] = job;
                }
                
                await DisplayAlert("Success", "Job marked as complete", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to mark job complete: {ex.Message}", "OK");
            }
        }

    private async void OnSearchClicked(object sender, EventArgs e)
    {
        var searchTerm = await DisplayPromptAsync("Search Jobs", "Enter search term:");
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            // Implement search functionality
            await DisplayAlert("Search", $"Searching for: {searchTerm}", "OK");
        }
    }

    private async void OnFilterClicked(object sender, EventArgs e)
    {
        var filterOptions = new[] { "All", "Pending", "In Progress", "Completed", "Cancelled" };
        var selectedFilter = await DisplayActionSheet("Filter Jobs", "Cancel", null, filterOptions);
        
        if (selectedFilter != "Cancel" && selectedFilter != null)
        {
            // Implement filter functionality
            await DisplayAlert("Filter", $"Filtering by: {selectedFilter}", "OK");
        }
    }

    private async void OnJobTypesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("JobTypeManagementPage");
    }

    private async void OnSignOutClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Sign out clicked from MainApplicationPage");
        Console.WriteLine("Sign out clicked from MainApplicationPage");
        
        var result = await DisplayAlert("Sign Out", "Are you sure you want to sign out?", "Yes", "No");
        if (result)
        {
            System.Diagnostics.Debug.WriteLine("User confirmed sign out");
            Console.WriteLine("User confirmed sign out");
            
            await _authService.SignOutAsync();
            
            // Clear authentication state
            Preferences.Set("IsAuthenticated", false);
            Preferences.Remove("UserId");
            Preferences.Remove("UserEmail");
            Preferences.Remove("RememberMe");
            
            // Create a new LoginPage and set it as the window page
            var loginPage = new LoginPage(_authService);
            
            // Use the recommended approach to update the window page
            if (Application.Current.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = loginPage;
            }
            
            System.Diagnostics.Debug.WriteLine("Sign out completed, returned to login page");
            Console.WriteLine("Sign out completed, returned to login page");
        }
    }
} 