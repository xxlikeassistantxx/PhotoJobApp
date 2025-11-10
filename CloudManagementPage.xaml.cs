using System.Collections.ObjectModel;
using System.Windows.Input;
using PhotoJobApp.Models;
using PhotoJobApp.Services;
using Microsoft.Maui.ApplicationModel;

namespace PhotoJobApp
{
    public partial class CloudManagementPage : ContentPage
    {
        private CloudJobTypeService? _cloudJobTypeService;
        private CloudJobService? _cloudJobService;
        private JobTypeService? _jobTypeService;
        private PhotoJobService? _photoJobService;
        private FirebaseAuthService? _authService;

        public ObservableCollection<JobType> CloudJobTypes { get; set; } = new();
        public ObservableCollection<PhotoJob> CloudJobs { get; set; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand PushAllCommand { get; }
        public ICommand PullAllCommand { get; }
        public ICommand DeleteJobTypeCommand { get; }
        public ICommand DeleteJobCommand { get; }

        public CloudManagementPage()
        {
            InitializeComponent();

            RefreshCommand = new Command(async () => await LoadCloudDataAsync());
            PushAllCommand = new Command(async () => await PushAllToCloudAsync());
            PullAllCommand = new Command(async () => await PullAllFromCloudAsync());
            DeleteJobTypeCommand = new Command<JobType>(async (jobType) => 
            {
                System.Diagnostics.Debug.WriteLine($"CloudManagementPage: DeleteJobTypeCommand triggered for job type: {jobType?.Name ?? "null"}");
                Console.WriteLine($"CloudManagementPage: DeleteJobTypeCommand triggered for job type: {jobType?.Name ?? "null"}");
                await DeleteJobTypeFromCloudAsync(jobType);
            });
            DeleteJobCommand = new Command<PhotoJob>(async (job) => await DeleteJobFromCloudAsync(job));

            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadCloudDataAsync();
        }

        private async Task InitializeServicesAsync()
        {
            // Get FirebaseAuthService from DI container
            if (_authService == null)
            {
                _authService = Application.Current?.Handler?.MauiContext?.Services.GetService<FirebaseAuthService>();
                if (_authService == null)
                {
                    await DisplayAlert("Error", "Authentication service is not available", "OK");
                    return;
                }
            }

            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                await DisplayAlert("Error", "You must be signed in to use cloud features", "OK");
                return;
            }

            // Initialize services with user ID
            _cloudJobTypeService = new CloudJobTypeService(currentUser.Id);
            _cloudJobService = new CloudJobService(currentUser.Id);
            _jobTypeService = await JobTypeService.CreateAsync(currentUser.Id);
            _photoJobService = new PhotoJobService();
        }

        private async Task LoadCloudDataAsync()
        {
            try
            {
                // Show loading indicators
                JobTypesLoadingIndicator.IsRunning = true;
                JobTypesLoadingIndicator.IsVisible = true;
                JobsLoadingIndicator.IsRunning = true;
                JobsLoadingIndicator.IsVisible = true;

                // Initialize services if needed
                if (_cloudJobTypeService == null || _cloudJobService == null)
                {
                    await InitializeServicesAsync();
                }

                // Show loading indicators
                JobTypesLoadingIndicator.IsRunning = true;
                JobTypesLoadingIndicator.IsVisible = true;
                JobsLoadingIndicator.IsRunning = true;
                JobsLoadingIndicator.IsVisible = true;

                // Load job types from cloud
                var cloudJobTypes = await _cloudJobTypeService.GetJobTypesAsync();
                System.Diagnostics.Debug.WriteLine($"CloudManagementPage: Retrieved {cloudJobTypes.Count} job types from cloud service");
                Console.WriteLine($"CloudManagementPage: Retrieved {cloudJobTypes.Count} job types from cloud service");
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    CloudJobTypes.Clear();
                    System.Diagnostics.Debug.WriteLine($"CloudManagementPage: Cleared CloudJobTypes collection");
                    Console.WriteLine($"CloudManagementPage: Cleared CloudJobTypes collection");
                    
                    foreach (var jobType in cloudJobTypes)
                    {
                        System.Diagnostics.Debug.WriteLine($"CloudManagementPage: Adding job type to display: {jobType.Name} (CloudId: {jobType.CloudId})");
                        Console.WriteLine($"CloudManagementPage: Adding job type to display: {jobType.Name} (CloudId: {jobType.CloudId})");
                        
                        // Make the name unique for testing
                        jobType.Name = $"{jobType.Name} ({jobType.CloudId.Substring(0, 8)})";
                        
                        CloudJobTypes.Add(jobType);
                        System.Diagnostics.Debug.WriteLine($"CloudManagementPage: Collection count after adding: {CloudJobTypes.Count}");
                        Console.WriteLine($"CloudManagementPage: Collection count after adding: {CloudJobTypes.Count}");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"CloudManagementPage: Final collection count on main thread: {CloudJobTypes.Count}");
                    Console.WriteLine($"CloudManagementPage: Final collection count on main thread: {CloudJobTypes.Count}");
                });

                // Load jobs from cloud
                var cloudJobs = await _cloudJobService.GetJobsAsync();
                CloudJobs.Clear();
                foreach (var job in cloudJobs)
                {
                    System.Diagnostics.Debug.WriteLine($"CloudManagementPage: Adding job to display: {job.Title} (ID: {job.Id})");
                    Console.WriteLine($"CloudManagementPage: Adding job to display: {job.Title} (ID: {job.Id})");
                    CloudJobs.Add(job);
                }

                // Update visibility
                NoJobTypesLabel.IsVisible = CloudJobTypes.Count == 0;
                NoJobsLabel.IsVisible = CloudJobs.Count == 0;

                System.Diagnostics.Debug.WriteLine($"CloudManagementPage: Final counts - JobTypes: {CloudJobTypes.Count}, Jobs: {CloudJobs.Count}");
                Console.WriteLine($"CloudManagementPage: Final counts - JobTypes: {CloudJobTypes.Count}, Jobs: {CloudJobs.Count}");
                System.Diagnostics.Debug.WriteLine($"CloudManagementPage: NoJobTypesLabel.IsVisible = {NoJobTypesLabel.IsVisible}");
                Console.WriteLine($"CloudManagementPage: NoJobTypesLabel.IsVisible = {NoJobTypesLabel.IsVisible}");
                System.Diagnostics.Debug.WriteLine($"CloudManagementPage: NoJobsLabel.IsVisible = {NoJobsLabel.IsVisible}");
                Console.WriteLine($"CloudManagementPage: NoJobsLabel.IsVisible = {NoJobsLabel.IsVisible}");

                await DisplayAlert("Success", $"Loaded {CloudJobTypes.Count} job types and {CloudJobs.Count} jobs from cloud", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CloudManagementPage: Error loading cloud data: {ex.Message}");
                Console.WriteLine($"CloudManagementPage: Error loading cloud data: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await DisplayAlert("Error", $"Failed to load cloud data: {ex.Message}", "OK");
            }
            finally
            {
                // Hide loading indicators
                JobTypesLoadingIndicator.IsRunning = false;
                JobTypesLoadingIndicator.IsVisible = false;
                JobsLoadingIndicator.IsRunning = false;
                JobsLoadingIndicator.IsVisible = false;

                // Force CollectionView to be visible and refresh
                JobTypesCollectionView.IsVisible = true;
                JobsCollectionView.IsVisible = true;
                
                System.Diagnostics.Debug.WriteLine($"CloudManagementPage: JobTypesCollectionView.IsVisible = {JobTypesCollectionView.IsVisible}");
                Console.WriteLine($"CloudManagementPage: JobTypesCollectionView.IsVisible = {JobTypesCollectionView.IsVisible}");
                System.Diagnostics.Debug.WriteLine($"CloudManagementPage: JobsCollectionView.IsVisible = {JobsCollectionView.IsVisible}");
                Console.WriteLine($"CloudManagementPage: JobsCollectionView.IsVisible = {JobsCollectionView.IsVisible}");

                // Force UI refresh for CollectionView
                JobTypesCollectionView.ItemsSource = null;
                JobTypesCollectionView.ItemsSource = CloudJobTypes;
                JobsCollectionView.ItemsSource = null;
                JobsCollectionView.ItemsSource = CloudJobs;
                System.Diagnostics.Debug.WriteLine("CloudManagementPage: Forced CollectionView refresh");
                Console.WriteLine("CloudManagementPage: Forced CollectionView refresh");
            }
        }

        private async Task PushAllToCloudAsync()
        {
            try
            {
                var result = await DisplayAlert("Confirm", "Push all local job types and jobs to cloud?", "Yes", "No");
                if (!result) return;

                // Get current user ID
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    await DisplayAlert("Error", "You must be signed in to use cloud features", "OK");
                    return;
                }

                // Initialize cloud services if needed
                if (_cloudJobTypeService == null || _cloudJobService == null)
                {
                    _cloudJobTypeService = new CloudJobTypeService(currentUser.Id);
                    _cloudJobService = new CloudJobService(currentUser.Id);
                }

                // Create services with user ID
                var jobTypeService = await JobTypeService.CreateAsync(currentUser.Id);
                var photoJobService = _photoJobService;

                // Push all job types
                var localJobTypes = await jobTypeService.GetJobTypesAsync();
                int jobTypesPushed = 0;
                foreach (var jobType in localJobTypes)
                {
                    await _cloudJobTypeService.SaveJobTypeAsync(jobType);
                    jobTypesPushed++;
                }

                // Push all jobs
                var localJobs = await photoJobService.GetJobsAsync();
                int jobsPushed = 0;
                foreach (var job in localJobs)
                {
                    await _cloudJobService.SaveJobAsync(job);
                    jobsPushed++;
                }

                await DisplayAlert("Success", $"Pushed {jobTypesPushed} job types and {jobsPushed} jobs to cloud", "OK");
                await LoadCloudDataAsync(); // Refresh the display
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CloudManagementPage: Error pushing to cloud: {ex.Message}");
                Console.WriteLine($"CloudManagementPage: Error pushing to cloud: {ex.Message}");
                await DisplayAlert("Error", $"Failed to push to cloud: {ex.Message}", "OK");
            }
        }

        private async Task PullAllFromCloudAsync()
        {
            try
            {
                var result = await DisplayAlert("Confirm", "Pull all cloud job types and jobs to local storage?", "Yes", "No");
                if (!result) return;

                // Initialize services if needed
                await InitializeServicesAsync();

                // Get current user ID
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    await DisplayAlert("Error", "You must be signed in to use cloud features", "OK");
                    return;
                }

                // Initialize cloud services if needed
                if (_cloudJobTypeService == null || _cloudJobService == null)
                {
                    _cloudJobTypeService = new CloudJobTypeService(currentUser.Id);
                    _cloudJobService = new CloudJobService(currentUser.Id);
                }

                // Create services with user ID
                var jobTypeService = await JobTypeService.CreateAsync(currentUser.Id);
                var photoJobService = _photoJobService ?? new PhotoJobService();

                // Pull job types
                var jobTypesPulled = await jobTypeService.PullFromCloudAsync();
                System.Diagnostics.Debug.WriteLine($"CloudManagementPage: Job types pull result: {jobTypesPulled}");
                Console.WriteLine($"CloudManagementPage: Job types pull result: {jobTypesPulled}");

                // Pull jobs
                var cloudJobs = await _cloudJobService.GetJobsAsync();
                int jobsPulled = 0;
                foreach (var cloudJob in cloudJobs)
                {
                    try
                    {
                        // Check if job already exists locally by CloudId instead of Id
                        var existingJob = await photoJobService.GetJobsAsync();
                        var jobExists = existingJob.Any(j => j.CloudId == cloudJob.CloudId);
                        
                        if (!jobExists)
                        {
                            // Reset the ID so it gets a new local ID
                            cloudJob.Id = 0;
                            
                            // Add new job locally
                            await photoJobService.SaveJobAsync(cloudJob);
                            jobsPulled++;
                            System.Diagnostics.Debug.WriteLine($"CloudManagementPage: Pulled job: {cloudJob.Title}");
                            Console.WriteLine($"CloudManagementPage: Pulled job: {cloudJob.Title}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"CloudManagementPage: Job already exists locally: {cloudJob.Title}");
                            Console.WriteLine($"CloudManagementPage: Job already exists locally: {cloudJob.Title}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"CloudManagementPage: Error pulling job {cloudJob.Title}: {ex.Message}");
                        Console.WriteLine($"CloudManagementPage: Error pulling job {cloudJob.Title}: {ex.Message}");
                    }
                }

                var message = $"Pull completed.\n";
                if (jobTypesPulled)
                {
                    message += "✓ Job types pulled from cloud\n";
                }
                else
                {
                    message += "ℹ No new job types found in cloud\n";
                }
                message += $"✓ {jobsPulled} new jobs pulled from cloud";

                await DisplayAlert("Success", message, "OK");
                await LoadCloudDataAsync(); // Refresh the display
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CloudManagementPage: Error pulling from cloud: {ex.Message}");
                Console.WriteLine($"CloudManagementPage: Error pulling from cloud: {ex.Message}");
                await DisplayAlert("Error", $"Failed to pull from cloud: {ex.Message}", "OK");
            }
        }

        private async Task DeleteJobTypeFromCloudAsync(JobType jobType)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"CloudManagementPage: DeleteJobTypeFromCloudAsync called for job type: {jobType.Name}");
                Console.WriteLine($"CloudManagementPage: DeleteJobTypeFromCloudAsync called for job type: {jobType.Name}");
                
                var result = await DisplayAlert("Confirm Delete", 
                    $"Are you sure you want to delete '{jobType.Name}' from the cloud?", "Yes", "No");
                if (!result) return;

                System.Diagnostics.Debug.WriteLine($"CloudManagementPage: User confirmed deletion for job type: {jobType.Name}");
                Console.WriteLine($"CloudManagementPage: User confirmed deletion for job type: {jobType.Name}");

                if (!string.IsNullOrEmpty(jobType.CloudId))
                {
                    System.Diagnostics.Debug.WriteLine($"CloudManagementPage: Deleting job type with CloudId: {jobType.CloudId}");
                    Console.WriteLine($"CloudManagementPage: Deleting job type with CloudId: {jobType.CloudId}");
                    
                    await _cloudJobTypeService.DeleteJobTypeAsync(jobType.CloudId);
                    CloudJobTypes.Remove(jobType);
                    await DisplayAlert("Success", "Job type deleted from cloud", "OK");
                    
                    System.Diagnostics.Debug.WriteLine($"CloudManagementPage: Job type deleted successfully");
                    Console.WriteLine($"CloudManagementPage: Job type deleted successfully");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"CloudManagementPage: Job type has no CloudId");
                    Console.WriteLine($"CloudManagementPage: Job type has no CloudId");
                    await DisplayAlert("Error", "Job type has no cloud ID", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CloudManagementPage: Error deleting job type: {ex.Message}");
                Console.WriteLine($"CloudManagementPage: Error deleting job type: {ex.Message}");
                await DisplayAlert("Error", $"Failed to delete job type: {ex.Message}", "OK");
            }
        }

        private async Task DeleteJobFromCloudAsync(PhotoJob job)
        {
            try
            {
                var result = await DisplayAlert("Confirm Delete", 
                    $"Are you sure you want to delete '{job.Title}' from the cloud?", "Yes", "No");
                if (!result) return;

                if (!string.IsNullOrEmpty(job.CloudId))
                {
                    // Initialize cloud service if needed
                    if (_cloudJobService == null)
                    {
                        var currentUser = await _authService.GetCurrentUserAsync();
                        if (currentUser == null)
                        {
                            await DisplayAlert("Error", "You must be signed in to use cloud features", "OK");
                            return;
                        }
                        _cloudJobService = new CloudJobService(currentUser.Id);
                    }

                    await _cloudJobService.DeleteJobAsync(job.CloudId);
                    CloudJobs.Remove(job);
                    await DisplayAlert("Success", "Job deleted from cloud", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "Job has no cloud ID", "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CloudManagementPage: Error deleting job: {ex.Message}");
                Console.WriteLine($"CloudManagementPage: Error deleting job: {ex.Message}");
                await DisplayAlert("Error", $"Failed to delete job: {ex.Message}", "OK");
            }
        }
    }
} 