using PhotoJobApp.Models;
using PhotoJobApp.Services;

namespace PhotoJobApp
{
    public partial class AddEditJobPage : ContentPage
    {
        private readonly PhotoJobService _photoJobService;
        private PhotoJob _job;
        private bool _isEditing;

        public AddEditJobPage(PhotoJob? job = null)
        {
            InitializeComponent();
            _photoJobService = new PhotoJobService();
            
            if (job != null)
            {
                _job = job;
                _isEditing = true;
                Title = "Edit Job";
            }
            else
            {
                _job = new PhotoJob();
                _isEditing = false;
                Title = "Add New Job";
            }

            BindingContext = _job;
            SetupStatusPicker();
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

                    await _photoJobService.SaveJobAsync(_job);
                    
                    string message = _isEditing ? "Job updated successfully!" : "Job added successfully!";
                    await DisplayAlert("Success", message, "OK");
                    
                    await Navigation.PopAsync();
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
            await Navigation.PopAsync();
        }
    }
} 