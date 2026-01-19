using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PhotoJobApp
{
    [QueryProperty(nameof(PhotoList), "PhotoList")]
    [QueryProperty(nameof(InitialIndex), "InitialIndex")]
    public partial class PhotoGalleryPage : ContentPage, INotifyPropertyChanged
    {
        private ObservableCollection<string> _photoList;
        private int _currentPhotoIndex;
        private bool _showNavigationButtons;

        public ObservableCollection<string> PhotoList
        {
            get => _photoList;
            set
            {
                _photoList = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentPhotoIndex));
                OnPropertyChanged(nameof(ShowNavigationButtons));
            }
        }

        public int InitialIndex
        {
            set
            {
                if (value >= 0 && value < PhotoList?.Count)
                {
                    _currentPhotoIndex = value;
                    OnPropertyChanged(nameof(CurrentPhotoIndex));
                }
            }
        }

        public int CurrentPhotoIndex
        {
            get => _currentPhotoIndex + 1; // Display 1-based index
            set
            {
                if (value >= 1 && value <= PhotoList?.Count)
                {
                    _currentPhotoIndex = value - 1; // Convert to 0-based index
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowNavigationButtons
        {
            get => _showNavigationButtons;
            set
            {
                _showNavigationButtons = value;
                OnPropertyChanged();
            }
        }

        public PhotoGalleryPage()
        {
            InitializeComponent();
            BindingContext = this;
            
            // Show navigation buttons on larger screens
            ShowNavigationButtons = DeviceDisplay.Current.MainDisplayInfo.Width > 800;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Set initial position if specified
            if (_currentPhotoIndex >= 0 && _currentPhotoIndex < PhotoList?.Count)
            {
                PhotoCarousel.Position = _currentPhotoIndex;
            }
            
            // Update the current photo index display
            OnPropertyChanged(nameof(CurrentPhotoIndex));
        }

        private void OnCurrentItemChanged(object sender, CurrentItemChangedEventArgs e)
        {
            if (e.CurrentItem != null && PhotoList != null)
            {
                _currentPhotoIndex = PhotoList.IndexOf(e.CurrentItem.ToString());
                OnPropertyChanged(nameof(CurrentPhotoIndex));
            }
        }

        private async void OnPhotoTapped(object sender, EventArgs e)
        {
            // Future enhancement: Add zoom functionality
            // For now, just show a brief message
            await DisplayAlert("Photo", "Tap to zoom functionality coming soon!", "OK");
        }

        private void OnPreviousPhotoClicked(object sender, EventArgs e)
        {
            if (PhotoCarousel.Position > 0)
            {
                PhotoCarousel.Position--;
            }
        }

        private void OnNextPhotoClicked(object sender, EventArgs e)
        {
            if (PhotoCarousel.Position < PhotoList.Count - 1)
            {
                PhotoCarousel.Position++;
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 