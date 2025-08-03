using PhotoJobApp.Models;
using PhotoJobApp.Services;

namespace PhotoJobApp
{
    public partial class JobDetailPage : ContentPage
    {
        private readonly PhotoJobService _photoJobService;
        private PhotoJob _job;

        public JobDetailPage(PhotoJob job)
        {
            InitializeComponent();
            _photoJobService = new PhotoJobService();
            _job = job;
            BindingContext = _job;
        }

        private async void OnEditClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddEditJobPage(_job));
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
                    await Navigation.PopAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to delete job: {ex.Message}", "OK");
                }
            }
        }
    }
} 