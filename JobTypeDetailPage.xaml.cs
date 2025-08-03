using PhotoJobApp.Models;

namespace PhotoJobApp
{
    public partial class JobTypeDetailPage : ContentPage
    {
        private JobType _jobType;

        public JobTypeDetailPage(JobType jobType)
        {
            InitializeComponent();
            _jobType = jobType;
            BindingContext = _jobType;
        }

        private async void OnEditClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddEditJobTypePage(_jobType));
        }

        private async void OnCreateJobClicked(object sender, EventArgs e)
        {
            // Create a new job with this job type pre-selected
            var newJob = new PhotoJob
            {
                JobTypeId = _jobType.Id,
                Status = _jobType.StatusList.FirstOrDefault() ?? "Pending"
            };
            
            await Navigation.PushAsync(new AddEditJobPage(newJob));
        }
    }
} 