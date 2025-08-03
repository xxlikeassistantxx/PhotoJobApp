using System.Collections.ObjectModel;
using PhotoJobApp.Models;
using PhotoJobApp.Services;

namespace PhotoJobApp
{
    public partial class JobTypeManagementPage : ContentPage
    {
        private readonly JobTypeService _jobTypeService;
        public ObservableCollection<JobType> JobTypes { get; set; }

        public JobTypeManagementPage()
        {
            InitializeComponent();
            _jobTypeService = new JobTypeService();
            JobTypes = new ObservableCollection<JobType>();
            BindingContext = this;
            LoadJobTypesAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadJobTypesAsync();
        }

        private async void LoadJobTypesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Loading job types from database...");
                var jobTypes = await _jobTypeService.GetJobTypesAsync();
                System.Diagnostics.Debug.WriteLine($"Loaded {jobTypes.Count} job types from database");
                
                JobTypes.Clear();
                foreach (var jobType in jobTypes)
                {
                    System.Diagnostics.Debug.WriteLine($"Adding job type: {jobType.Name}");
                    JobTypes.Add(jobType);
                }
                
                System.Diagnostics.Debug.WriteLine($"Total job types in collection: {JobTypes.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading job types: {ex.Message}");
                await DisplayAlert("Error", $"Failed to load job types: {ex.Message}", "OK");
            }
        }

        private async void OnAddJobTypeClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddEditJobTypePage());
        }

        private async void OnJobTypeTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is JobType jobType)
            {
                await Navigation.PushAsync(new JobTypeDetailPage(jobType));
            }
        }

        private async void OnEditJobTypeClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is JobType jobType)
            {
                await Navigation.PushAsync(new AddEditJobTypePage(jobType));
            }
        }

        private async void OnDeleteJobTypeClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is JobType jobType)
            {
                await DeleteJobTypeAsync(jobType);
            }
        }

        private async Task DeleteJobTypeAsync(JobType jobType)
        {
            var confirm = await DisplayAlert("Confirm Delete", 
                $"Are you sure you want to delete '{jobType.Name}'?\n\nThis will also affect any jobs using this type.", "Yes", "No");
            
            if (confirm)
            {
                try
                {
                    await _jobTypeService.DeleteJobTypeAsync(jobType);
                    JobTypes.Remove(jobType);
                    await DisplayAlert("Success", "Job type deleted successfully", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to delete job type: {ex.Message}", "OK");
                }
            }
        }
    }
} 