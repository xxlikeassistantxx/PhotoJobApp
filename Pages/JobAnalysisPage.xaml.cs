using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Maui.ApplicationModel;
using PhotoJobApp.Models;
using PhotoJobApp.Services;

namespace PhotoJobApp
{
    public partial class JobAnalysisPage : ContentPage, INotifyPropertyChanged
    {
        private readonly PhotoJobService _photoJobService;
        private JobTypeService? _jobTypeService;
        private readonly FirebaseAuthService _authService;
        private List<PhotoJob> _allJobs = new List<PhotoJob>();
        private List<JobType> _allJobTypes = new List<JobType>();

        // Properties for data binding
        public string TotalJobs { get; set; } = "0";
        public string TotalRevenue { get; set; } = "$0.00";
        public string AverageJobValue { get; set; } = "$0.00";
        public string FilteredJobCount { get; set; } = "0";
        public string FilteredRevenue { get; set; } = "$0.00";
        public bool IsFiltered { get; set; } = false;
        public ObservableCollection<StatusBreakdownItem> StatusBreakdown { get; set; } = new ObservableCollection<StatusBreakdownItem>();
        public ObservableCollection<JobTypeBreakdownItem> JobTypeBreakdown { get; set; } = new ObservableCollection<JobTypeBreakdownItem>();
        public ObservableCollection<JobTypeFilterItem> JobTypeFilters { get; set; } = new ObservableCollection<JobTypeFilterItem>();

        // Custom field search properties
        private string _customFieldSearchTerm = string.Empty;
        private ObservableCollection<CustomFieldSearchResult> _customFieldSearchResults = new ObservableCollection<CustomFieldSearchResult>();
        private bool _hasCustomFieldSearchResults = false;
        private string _customFieldSearchCount = "0";
        private string _customFieldSearchRevenue = "$0.00";

        // New custom field search properties
        private JobTypeFilterItem? _selectedJobTypeForSearch = null;
        private ObservableCollection<CustomField> _availableCustomFields = new ObservableCollection<CustomField>();
        private CustomField? _selectedCustomField = null;
        private string _customFieldSearchValue = string.Empty;
        private string _customFieldSearchAverage = "$0.00";
        private bool _hasCustomFieldsForSelectedJobType = false;

        // Keep old properties for backward compatibility
        public string CustomFieldSearchTerm 
        { 
            get => _customFieldSearchTerm; 
            set 
            { 
                _customFieldSearchTerm = value; 
                OnPropertyChanged();
            } 
        }
        
        public ObservableCollection<CustomFieldSearchResult> CustomFieldSearchResults 
        { 
            get => _customFieldSearchResults; 
            set 
            { 
                _customFieldSearchResults = value; 
                OnPropertyChanged(); 
            } 
        }
        
        public bool HasCustomFieldSearchResults 
        { 
            get => _hasCustomFieldSearchResults; 
            set 
            { 
                _hasCustomFieldSearchResults = value; 
                OnPropertyChanged(); 
            } 
        }
        
        public string CustomFieldSearchCount 
        { 
            get => _customFieldSearchCount; 
            set 
            { 
                _customFieldSearchCount = value; 
                OnPropertyChanged(); 
            } 
        }
        
        public string CustomFieldSearchRevenue 
        { 
            get => _customFieldSearchRevenue; 
            set 
            { 
                _customFieldSearchRevenue = value; 
                OnPropertyChanged(); 
            } 
        }

        public JobTypeFilterItem? SelectedJobTypeForSearch
        {
            get => _selectedJobTypeForSearch;
            set
            {
                _selectedJobTypeForSearch = value;
                OnPropertyChanged();
                LoadCustomFieldsForSelectedJobType();
            }
        }

        public ObservableCollection<CustomField> AvailableCustomFields
        {
            get => _availableCustomFields;
            set
            {
                _availableCustomFields = value;
                OnPropertyChanged();
            }
        }

        public CustomField? SelectedCustomField
        {
            get => _selectedCustomField;
            set
            {
                _selectedCustomField = value;
                OnPropertyChanged();
                ClearCustomFieldSearch();
            }
        }

        public string CustomFieldSearchValue
        {
            get => _customFieldSearchValue;
            set
            {
                _customFieldSearchValue = value;
                OnPropertyChanged();
            }
        }

        public string CustomFieldSearchAverage
        {
            get => _customFieldSearchAverage;
            set
            {
                _customFieldSearchAverage = value;
                OnPropertyChanged();
            }
        }

        public bool HasCustomFieldsForSelectedJobType
        {
            get => _hasCustomFieldsForSelectedJobType;
            set
            {
                _hasCustomFieldsForSelectedJobType = value;
                OnPropertyChanged();
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public JobAnalysisPage(FirebaseAuthService authService = null)
        {
            InitializeComponent();
            _photoJobService = new PhotoJobService();
            _authService = authService ?? new FirebaseAuthService();
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadAnalysisData();
        }

        private async Task LoadAnalysisData()
        {
            try
            {
                // Initialize JobTypeService if needed
                if (_jobTypeService == null)
                {
                    var userId = _authService.GetCurrentUserAsync().Result?.Id ?? "";
                    _jobTypeService = await JobTypeService.CreateAsync(userId);
                }

                // Load all jobs
                _allJobs = await _photoJobService.GetJobsAsync();
                
                // Load job types for each job
                foreach (var job in _allJobs)
                {
                    if (job.JobTypeId > 0 && _jobTypeService != null)
                    {
                        job.JobType = await _jobTypeService.GetJobTypeAsync(job.JobTypeId);
                    }
                }

                // Load all job types
                if (_jobTypeService != null)
                {
                    _allJobTypes = await _jobTypeService.GetJobTypesAsync();
                }

                // Populate job type filters
                PopulateJobTypeFilters();

                // Calculate and display statistics
                CalculateOverallStatistics();
                CalculateStatusBreakdown();
                CalculateJobTypeBreakdown();

                // Clear any existing custom field search
                ClearCustomFieldSearch();
                SelectedJobTypeForSearch = null;
                SelectedCustomField = null;
                CustomFieldSearchValue = string.Empty;
                AvailableCustomFields.Clear();
                HasCustomFieldsForSelectedJobType = false;

                // Update UI
                OnPropertyChanged(nameof(TotalJobs));
                OnPropertyChanged(nameof(TotalRevenue));
                OnPropertyChanged(nameof(AverageJobValue));
                OnPropertyChanged(nameof(StatusBreakdown));
                OnPropertyChanged(nameof(JobTypeBreakdown));
                OnPropertyChanged(nameof(JobTypeFilters));
                OnPropertyChanged(nameof(AvailableCustomFields));
                OnPropertyChanged(nameof(HasCustomFieldsForSelectedJobType));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load analysis data: {ex.Message}", "OK");
            }
        }

        private void PopulateJobTypeFilters()
        {
            JobTypeFilters.Clear();
            
            // Add "All Job Types" option
            JobTypeFilters.Add(new JobTypeFilterItem
            {
                JobTypeId = -1,
                JobTypeName = "All Job Types",
                IsSelected = true
            });
            
            // Add individual job types
            foreach (var jobType in _allJobTypes)
            {
                JobTypeFilters.Add(new JobTypeFilterItem
                {
                    JobTypeId = jobType.Id,
                    JobTypeName = jobType.Name,
                    IsSelected = false
                });
            }
        }

        private List<PhotoJob> GetFilteredJobs()
        {
            // Check if "All Job Types" is selected
            var allJobTypesSelected = JobTypeFilters.Any(x => x.JobTypeId == -1 && x.IsSelected);
            
            if (allJobTypesSelected)
            {
                // If "All Job Types" is selected, return all jobs
                return _allJobs;
            }
            else
            {
                // Get selected job type IDs (excluding "All Job Types")
                var selectedJobTypeIds = JobTypeFilters
                    .Where(x => x.IsSelected && x.JobTypeId != -1)
                    .Select(x => x.JobTypeId)
                    .ToList();

                if (selectedJobTypeIds.Any())
                {
                    return _allJobs.Where(j => selectedJobTypeIds.Contains(j.JobTypeId)).ToList();
                }
                else
                {
                    // If no job types are selected, return all jobs
                    return _allJobs;
                }
            }
        }

        private void CalculateOverallStatistics()
        {
            var filteredJobs = GetFilteredJobs();
            var totalJobs = filteredJobs.Count;
            var totalRevenue = filteredJobs.Sum(j => j.Price);
            var averageJobValue = totalJobs > 0 ? totalRevenue / totalJobs : 0;

            TotalJobs = totalJobs.ToString();
            TotalRevenue = $"${totalRevenue:N2}";
            AverageJobValue = $"${averageJobValue:N2}";
        }

        private void CalculateStatusBreakdown()
        {
            StatusBreakdown.Clear();
            
            var filteredJobs = GetFilteredJobs();
            var statusGroups = filteredJobs.GroupBy(j => j.Status).ToList();
            
            foreach (var group in statusGroups)
            {
                var status = group.Key;
                var count = group.Count();
                var revenue = group.Sum(j => j.Price);
                
                StatusBreakdown.Add(new StatusBreakdownItem
                {
                    Status = status,
                    Count = count,
                    Revenue = $"${revenue:N2}"
                });
            }
        }

        private void CalculateJobTypeBreakdown()
        {
            JobTypeBreakdown.Clear();
            
            var filteredJobs = GetFilteredJobs();
            var jobTypeGroups = filteredJobs.Where(j => j.JobType != null).GroupBy(j => j.JobType.Name).ToList();
            
            foreach (var group in jobTypeGroups)
            {
                var jobTypeName = group.Key;
                var count = group.Count();
                var revenue = group.Sum(j => j.Price);
                
                JobTypeBreakdown.Add(new JobTypeBreakdownItem
                {
                    JobTypeName = jobTypeName,
                    Count = count,
                    Revenue = $"${revenue:N2}"
                });
            }
        }

        private void OnJobTypeFilterChanged(object sender, CheckedChangedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox?.BindingContext is JobTypeFilterItem filterItem)
            {
                // If "All Job Types" is selected, unselect all others
                if (filterItem.JobTypeId == -1 && e.Value)
                {
                    foreach (var item in JobTypeFilters)
                    {
                        if (item.JobTypeId != -1)
                        {
                            item.IsSelected = false;
                        }
                    }
                    filterItem.IsSelected = true;
                }
                // If a specific job type is selected, unselect "All Job Types"
                else if (filterItem.JobTypeId != -1 && e.Value)
                {
                    var allJobTypesItem = JobTypeFilters.FirstOrDefault(x => x.JobTypeId == -1);
                    if (allJobTypesItem != null)
                    {
                        allJobTypesItem.IsSelected = false;
                    }
                }
                
                ApplyJobTypeFilter();
                
                // Update UI
                OnPropertyChanged(nameof(JobTypeFilters));
            }
        }

        private void FilterJobsByJobType(List<int> selectedJobTypeIds)
        {
            var filteredJobs = _allJobs.Where(j => selectedJobTypeIds.Contains(j.JobTypeId)).ToList();
            var filteredCount = filteredJobs.Count;
            var filteredRevenue = filteredJobs.Sum(j => j.Price);

            FilteredJobCount = filteredCount.ToString();
            FilteredRevenue = $"${filteredRevenue:N2}";
            IsFiltered = true;

            OnPropertyChanged(nameof(FilteredJobCount));
            OnPropertyChanged(nameof(FilteredRevenue));
            OnPropertyChanged(nameof(IsFiltered));
        }

        private void ApplyJobTypeFilter()
        {
            // Check if "All Job Types" is selected
            var allJobTypesSelected = JobTypeFilters.Any(x => x.JobTypeId == -1 && x.IsSelected);
            
            if (allJobTypesSelected)
            {
                ClearFilter();
            }
            else
            {
                var selectedJobTypeIds = JobTypeFilters
                    .Where(x => x.IsSelected && x.JobTypeId != -1)
                    .Select(x => x.JobTypeId)
                    .ToList();

                if (selectedJobTypeIds.Any())
                {
                    FilterJobsByJobType(selectedJobTypeIds);
                }
                else
                {
                    ClearFilter();
                }
            }

            // Recalculate all statistics based on filtered jobs
            CalculateOverallStatistics();
            CalculateStatusBreakdown();
            CalculateJobTypeBreakdown();

            // Update UI
            OnPropertyChanged(nameof(TotalJobs));
            OnPropertyChanged(nameof(TotalRevenue));
            OnPropertyChanged(nameof(AverageJobValue));
            OnPropertyChanged(nameof(StatusBreakdown));
            OnPropertyChanged(nameof(JobTypeBreakdown));
        }

        private void ClearFilter()
        {
            IsFiltered = false;
            OnPropertyChanged(nameof(IsFiltered));
            
            // Recalculate all statistics based on all jobs
            CalculateOverallStatistics();
            CalculateStatusBreakdown();
            CalculateJobTypeBreakdown();

            // Update UI
            OnPropertyChanged(nameof(TotalJobs));
            OnPropertyChanged(nameof(TotalRevenue));
            OnPropertyChanged(nameof(AverageJobValue));
            OnPropertyChanged(nameof(StatusBreakdown));
            OnPropertyChanged(nameof(JobTypeBreakdown));
        }

        private void OnShowAllJobsClicked(object sender, EventArgs e)
        {
            // Select "All Job Types" and unselect everything else
            foreach (var item in JobTypeFilters)
            {
                item.IsSelected = item.JobTypeId == -1;
            }
            ClearFilter();
            
            // Update UI
            OnPropertyChanged(nameof(JobTypeFilters));
        }

        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await LoadAnalysisData();
        }

        // Custom field search methods
        private void OnCustomFieldSearchCompleted(object sender, EventArgs e)
        {
            PerformCustomFieldSearch();
        }

        private void OnCustomFieldSearchClicked(object sender, EventArgs e)
        {
            PerformCustomFieldSearch();
        }

        private void PerformCustomFieldSearch()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CustomFieldSearchValue) || SelectedCustomField == null)
                {
                    ClearCustomFieldSearch();
                    return;
                }

                var searchTerm = CustomFieldSearchValue.Trim().ToLower();
                var targetFieldName = SelectedCustomField.Name;
                var searchResults = new List<CustomFieldSearchResult>();

                // Filter jobs by selected job type if specified
                var jobsToSearch = _allJobs;
                if (SelectedJobTypeForSearch != null && SelectedJobTypeForSearch.JobTypeId != -1)
                {
                    jobsToSearch = _allJobs.Where(j => j.JobTypeId == SelectedJobTypeForSearch.JobTypeId).ToList();
                }

                foreach (var job in jobsToSearch)
                {
                    if (string.IsNullOrEmpty(job.CustomFieldValues)) continue;

                    try
                    {
                        var customFieldValues = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(job.CustomFieldValues);
                        if (customFieldValues == null || customFieldValues.Count == 0) continue;

                        // Check if the job has the target custom field
                        if (customFieldValues.ContainsKey(targetFieldName))
                        {
                            var fieldValue = customFieldValues[targetFieldName]?.ToLower() ?? string.Empty;

                            // Check if the field value contains the search term
                            if (!string.IsNullOrEmpty(fieldValue) && fieldValue.Contains(searchTerm))
                            {
                                searchResults.Add(new CustomFieldSearchResult
                                {
                                    JobTitle = job.Title ?? "Untitled Job",
                                    CustomFieldName = targetFieldName,
                                    CustomFieldValue = customFieldValues[targetFieldName] ?? "No Value",
                                    JobPrice = job.Price,
                                    JobId = job.Id
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error parsing custom field values for job {job.Id}: {ex.Message}");
                        // Continue with other jobs even if one fails
                    }
                }

                // Update search results
                CustomFieldSearchResults.Clear();
                foreach (var result in searchResults)
                {
                    CustomFieldSearchResults.Add(result);
                }

                // Update search statistics
                var totalRevenue = searchResults.Sum(r => r.JobPrice);
                var averageJobValue = searchResults.Count > 0 ? totalRevenue / searchResults.Count : 0;
                
                CustomFieldSearchCount = searchResults.Count.ToString();
                CustomFieldSearchRevenue = $"${totalRevenue:N2}";
                CustomFieldSearchAverage = $"${averageJobValue:N2}";
                HasCustomFieldSearchResults = searchResults.Count > 0;

                // Update UI
                OnPropertyChanged(nameof(CustomFieldSearchResults));
                OnPropertyChanged(nameof(CustomFieldSearchCount));
                OnPropertyChanged(nameof(CustomFieldSearchRevenue));
                OnPropertyChanged(nameof(CustomFieldSearchAverage));
                OnPropertyChanged(nameof(HasCustomFieldSearchResults));

                // Show a message if no results found
                if (searchResults.Count == 0 && !string.IsNullOrWhiteSpace(CustomFieldSearchValue))
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await DisplayAlert("Search Results", $"No jobs found with '{targetFieldName}' containing '{CustomFieldSearchValue}'.", "OK");
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error performing custom field search: {ex.Message}");
                ClearCustomFieldSearch();
            }
        }

        private void ClearCustomFieldSearch()
        {
            CustomFieldSearchResults.Clear();
            CustomFieldSearchCount = "0";
            CustomFieldSearchRevenue = "$0.00";
            CustomFieldSearchAverage = "$0.00";
            HasCustomFieldSearchResults = false;

            OnPropertyChanged(nameof(CustomFieldSearchResults));
            OnPropertyChanged(nameof(CustomFieldSearchCount));
            OnPropertyChanged(nameof(CustomFieldSearchRevenue));
            OnPropertyChanged(nameof(CustomFieldSearchAverage));
            OnPropertyChanged(nameof(HasCustomFieldSearchResults));
        }

        // New methods for enhanced custom field search
        private void LoadCustomFieldsForSelectedJobType()
        {
            try
            {
                AvailableCustomFields.Clear();
                SelectedCustomField = null;
                HasCustomFieldsForSelectedJobType = false;

                if (SelectedJobTypeForSearch == null || SelectedJobTypeForSearch.JobTypeId == -1)
                {
                    // "All Job Types" selected - show custom fields from all job types
                    var allCustomFields = new HashSet<string>();
                    
                    foreach (var jobType in _allJobTypes)
                    {
                        if (!string.IsNullOrEmpty(jobType.CustomFields))
                        {
                            try
                            {
                                var customFields = System.Text.Json.JsonSerializer.Deserialize<List<CustomField>>(jobType.CustomFields);
                                if (customFields != null)
                                {
                                    foreach (var field in customFields)
                                    {
                                        allCustomFields.Add(field.Name);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error parsing custom fields for job type {jobType.Id}: {ex.Message}");
                            }
                        }
                    }

                    foreach (var fieldName in allCustomFields.OrderBy(f => f))
                    {
                        AvailableCustomFields.Add(new CustomField { Name = fieldName, Type = "text" });
                    }
                }
                else
                {
                    // Specific job type selected
                    var selectedJobType = _allJobTypes.FirstOrDefault(jt => jt.Id == SelectedJobTypeForSearch.JobTypeId);
                    if (selectedJobType != null && !string.IsNullOrEmpty(selectedJobType.CustomFields))
                    {
                        try
                        {
                            var customFields = System.Text.Json.JsonSerializer.Deserialize<List<CustomField>>(selectedJobType.CustomFields);
                            if (customFields != null && customFields.Count > 0)
                            {
                                foreach (var field in customFields.OrderBy(f => f.Name))
                                {
                                    AvailableCustomFields.Add(field);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error parsing custom fields for job type {selectedJobType.Id}: {ex.Message}");
                        }
                    }
                }

                HasCustomFieldsForSelectedJobType = AvailableCustomFields.Count > 0;
                OnPropertyChanged(nameof(AvailableCustomFields));
                OnPropertyChanged(nameof(HasCustomFieldsForSelectedJobType));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading custom fields: {ex.Message}");
            }
        }

        private void OnJobTypeForSearchChanged(object sender, EventArgs e)
        {
            LoadCustomFieldsForSelectedJobType();
        }

        private void OnCustomFieldSelectionChanged(object sender, EventArgs e)
        {
            ClearCustomFieldSearch();
        }
    }

    public class StatusBreakdownItem
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Revenue { get; set; } = string.Empty;
    }

    public class JobTypeBreakdownItem
    {
        public string JobTypeName { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Revenue { get; set; } = string.Empty;
    }

    public class JobTypeFilterItem : INotifyPropertyChanged
    {
        private int _jobTypeId;
        private string _jobTypeName = string.Empty;
        private bool _isSelected = false;

        public int JobTypeId 
        { 
            get => _jobTypeId; 
            set 
            { 
                _jobTypeId = value; 
                OnPropertyChanged(); 
            } 
        }
        
        public string JobTypeName 
        { 
            get => _jobTypeName; 
            set 
            { 
                _jobTypeName = value; 
                OnPropertyChanged(); 
            } 
        }
        
        public bool IsSelected 
        { 
            get => _isSelected; 
            set 
            { 
                _isSelected = value; 
                OnPropertyChanged(); 
            } 
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CustomFieldSearchResult
    {
        public string JobTitle { get; set; } = string.Empty;
        public string CustomFieldName { get; set; } = string.Empty;
        public string CustomFieldValue { get; set; } = string.Empty;
        public decimal JobPrice { get; set; }
        public int JobId { get; set; }
    }
} 