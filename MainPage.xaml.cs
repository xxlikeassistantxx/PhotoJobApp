using System.Collections.ObjectModel;
using PhotoJobApp.Models;
using PhotoJobApp.Services;
using CommunityToolkit.Maui.Views;

namespace PhotoJobApp
{
    public partial class MainPage : ContentPage
    {
        private readonly PhotoJobService _photoJobService;
        public ObservableCollection<PhotoJob> Jobs { get; set; }

        public MainPage()
        {
            InitializeComponent();
            _photoJobService = new PhotoJobService();
            Jobs = new ObservableCollection<PhotoJob>();
            BindingContext = this;
            LoadJobsAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadJobsAsync();
        }

        private async void LoadJobsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Loading jobs from database...");
                var jobs = await _photoJobService.GetJobsAsync();
                System.Diagnostics.Debug.WriteLine($"Loaded {jobs.Count} jobs from database");
                
                Jobs.Clear();
                foreach (var job in jobs)
                {
                    System.Diagnostics.Debug.WriteLine($"Adding job: {job.Title} (Created: {job.CreatedDate})");
                    Jobs.Add(job);
                }
                
                System.Diagnostics.Debug.WriteLine($"Total jobs in collection: {Jobs.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading jobs: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                await DisplayAlert("Error", $"Failed to load jobs: {ex.Message}", "OK");
            }
        }

        private async void OnAddJobClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddEditJobPage());
        }

        private async void OnJobTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is PhotoJob job)
            {
                await Navigation.PushAsync(new JobDetailPage(job));
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
                        await Navigation.PushAsync(new AddEditJobPage(job));
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
                
                // Refresh the list
                LoadJobsAsync();
                await DisplayAlert("Success", "Job marked as completed", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to update job: {ex.Message}", "OK");
            }
        }

        private async void OnSearchClicked(object sender, EventArgs e)
        {
            string searchTerm = await DisplayPromptAsync("Search Jobs", "Enter search term:");
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                try
                {
                    var searchResults = await _photoJobService.SearchJobsAsync(searchTerm);
                    Jobs.Clear();
                    foreach (var job in searchResults)
                    {
                        Jobs.Add(job);
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Search failed: {ex.Message}", "OK");
                }
            }
        }

        private async void OnFilterClicked(object sender, EventArgs e)
        {
            var status = await DisplayActionSheet("Filter by Status", "Cancel", null, 
                "All", "Pending", "In Progress", "Completed", "Cancelled");
            
            if (status != "Cancel" && status != "All")
            {
                try
                {
                    var filteredJobs = await _photoJobService.GetJobsByStatusAsync(status);
                    Jobs.Clear();
                    foreach (var job in filteredJobs)
                    {
                        Jobs.Add(job);
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Filter failed: {ex.Message}", "OK");
                }
            }
            else if (status == "All")
            {
                LoadJobsAsync();
            }
        }

        private async void OnJobTypesClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new JobTypeManagementPage());
        }
    }
}
