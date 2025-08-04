using System.Collections.ObjectModel;
using PhotoJobApp.Models;
using PhotoJobApp.Services;

namespace PhotoJobApp
{
    public partial class JobTypeManagementPage : ContentPage
    {
        private JobTypeService _jobTypeService;
        public ObservableCollection<JobType> JobTypes { get; set; }

        public JobTypeManagementPage()
        {
            InitializeComponent();
            JobTypes = new ObservableCollection<JobType>();
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("JobTypeManagementPage: OnAppearing called");
                Console.WriteLine("JobTypeManagementPage: OnAppearing called");
                
                var authService = new FirebaseAuthService();
                var currentUser = await authService.GetCurrentUserAsync();
                var userId = currentUser?.Id;
                
                System.Diagnostics.Debug.WriteLine($"JobTypeManagementPage: Current user ID: {userId ?? "null"}");
                Console.WriteLine($"JobTypeManagementPage: Current user ID: {userId ?? "null"}");
                
                _jobTypeService = await JobTypeService.CreateAsync(userId);
                
                System.Diagnostics.Debug.WriteLine($"JobTypeManagementPage: JobTypeService created with cloud sync: {userId != null}");
                Console.WriteLine($"JobTypeManagementPage: JobTypeService created with cloud sync: {userId != null}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JobTypeManagementPage: Error in OnAppearing: {ex.Message}");
                Console.WriteLine($"JobTypeManagementPage: Error in OnAppearing: {ex.Message}");
            }
            
            await LoadJobTypesAsync();
        }

        private async Task LoadJobTypesAsync()
        {
            try
            {
                var jobTypes = await _jobTypeService.GetJobTypesAsync();
                JobTypes.Clear();
                foreach (var jobType in jobTypes)
                {
                    JobTypes.Add(jobType);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load job types: {ex.Message}", "OK");
            }
        }

        private async void OnAddJobTypeClicked(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Add Job Type button clicked");
                Console.WriteLine("Add Job Type button clicked");
                
                // Try Shell navigation first
                await Shell.Current.GoToAsync("AddEditJobTypePage");
                System.Diagnostics.Debug.WriteLine("Shell navigation to AddEditJobTypePage successful");
                Console.WriteLine("Shell navigation to AddEditJobTypePage successful");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Shell navigation failed: {ex.Message}");
                Console.WriteLine($"Shell navigation failed: {ex.Message}");
                
                // Fallback: Create AddEditJobTypePage directly and set as window page
                try
                {
                    if (Application.Current.Windows.Count > 0)
                    {
                        var addEditJobTypePage = new AddEditJobTypePage();
                        Application.Current.Windows[0].Page = addEditJobTypePage;
                        
                        System.Diagnostics.Debug.WriteLine("AddEditJobTypePage navigation completed via fallback");
                        Console.WriteLine("AddEditJobTypePage navigation completed via fallback");
                    }
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Fallback navigation failed: {fallbackEx.Message}");
                    Console.WriteLine($"Fallback navigation failed: {fallbackEx.Message}");
                    await DisplayAlert("Navigation Error", "Unable to open Add Job Type page. Please try again.", "OK");
                }
            }
        }

        private async void OnPullFromCloudClicked(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Pull from cloud button clicked");
                Console.WriteLine("Pull from cloud button clicked");
                
                if (_jobTypeService != null)
                {
                    System.Diagnostics.Debug.WriteLine("JobTypeService is available, pulling from cloud...");
                    Console.WriteLine("JobTypeService is available, pulling from cloud...");
                    
                    var success = await _jobTypeService.PullFromCloudAsync();
                    System.Diagnostics.Debug.WriteLine($"Pull result: {success}");
                    Console.WriteLine($"Pull result: {success}");
                    
                    if (success)
                    {
                        await DisplayAlert("Pull Complete", "Job types have been pulled from the cloud.", "OK");
                        await LoadJobTypesAsync(); // Reload the list
                    }
                    else
                    {
                        await DisplayAlert("Pull Failed", "Failed to pull job types from the cloud. Please check your internet connection and Firebase configuration.", "OK");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("JobTypeService is null!");
                    Console.WriteLine("JobTypeService is null!");
                    await DisplayAlert("Error", "Job type service not available.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Pull error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Pull error stack trace: {ex.StackTrace}");
                Console.WriteLine($"Pull error: {ex.Message}");
                Console.WriteLine($"Pull error stack trace: {ex.StackTrace}");
                await DisplayAlert("Error", $"Pull failed: {ex.Message}", "OK");
            }
        }

        private async void OnSyncClicked(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Sync button clicked");
                Console.WriteLine("Sync button clicked");
                
                if (_jobTypeService != null)
                {
                    System.Diagnostics.Debug.WriteLine("JobTypeService is available, starting sync...");
                    Console.WriteLine("JobTypeService is available, starting sync...");
                    
                    var success = await _jobTypeService.SyncWithCloudAsync();
                    System.Diagnostics.Debug.WriteLine($"Sync result: {success}");
                    Console.WriteLine($"Sync result: {success}");
                    
                    if (success)
                    {
                        await DisplayAlert("Sync Complete", "Job types have been synchronized with the cloud.", "OK");
                        await LoadJobTypesAsync(); // Reload the list
                    }
                    else
                    {
                        await DisplayAlert("Sync Failed", "Failed to synchronize job types with the cloud. Please check your internet connection and Firebase configuration.", "OK");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("JobTypeService is null!");
                    Console.WriteLine("JobTypeService is null!");
                    await DisplayAlert("Error", "Job type service not available.", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sync error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Sync error stack trace: {ex.StackTrace}");
                Console.WriteLine($"Sync error: {ex.Message}");
                Console.WriteLine($"Sync error stack trace: {ex.StackTrace}");
                await DisplayAlert("Error", $"Sync failed: {ex.Message}", "OK");
            }
        }

        private async void OnJobTypeTapped(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is JobType selectedJobType)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Job type tapped: {selectedJobType.Name} (ID: {selectedJobType.Id})");
                    Console.WriteLine($"Job type tapped: {selectedJobType.Name} (ID: {selectedJobType.Id})");
                    
                    // Try Shell navigation first
                    await Shell.Current.GoToAsync($"JobTypeDetailPage?JobType={selectedJobType.Id}");
                    System.Diagnostics.Debug.WriteLine("Shell navigation to JobTypeDetailPage successful");
                    Console.WriteLine("Shell navigation to JobTypeDetailPage successful");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Shell navigation failed: {ex.Message}");
                    Console.WriteLine($"Shell navigation failed: {ex.Message}");
                    
                    // Fallback: Create JobTypeDetailPage directly and set as window page
                    try
                    {
                        if (Application.Current.Windows.Count > 0)
                        {
                            var jobTypeDetailPage = new JobTypeDetailPage();
                            // Set the JobTypeId property manually
                            jobTypeDetailPage.GetType().GetProperty("JobTypeId")?.SetValue(jobTypeDetailPage, selectedJobType.Id.ToString());
                            Application.Current.Windows[0].Page = jobTypeDetailPage;
                            
                            System.Diagnostics.Debug.WriteLine("JobTypeDetailPage navigation completed via fallback");
                            Console.WriteLine("JobTypeDetailPage navigation completed via fallback");
                        }
                    }
                    catch (Exception fallbackEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Fallback navigation failed: {fallbackEx.Message}");
                        Console.WriteLine($"Fallback navigation failed: {fallbackEx.Message}");
                        await DisplayAlert("Navigation Error", "Unable to open Job Type details. Please try again.", "OK");
                    }
                }

                // Deselect the item
                ((CollectionView)sender).SelectedItem = null;
            }
        }
    }
} 