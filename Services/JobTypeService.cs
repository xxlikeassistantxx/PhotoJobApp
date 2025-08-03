using SQLite;
using PhotoJobApp.Models;
using System.Collections.ObjectModel;

namespace PhotoJobApp.Services
{
    public class JobTypeService
    {
        private readonly SQLiteAsyncConnection _database;
        private readonly string _databasePath;

        public JobTypeService()
        {
            _databasePath = Path.Combine(FileSystem.AppDataDirectory, "PhotoJobs.db3");
            _database = new SQLiteAsyncConnection(_databasePath);
            InitializeDatabaseAsync().Wait();
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                await _database.CreateTableAsync<JobType>();
                
                // Add some default job types if the table is empty
                var existingTypes = await _database.Table<JobType>().CountAsync();
                if (existingTypes == 0)
                {
                    await AddDefaultJobTypesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JobType database initialization error: {ex.Message}");
            }
        }

        private async Task AddDefaultJobTypesAsync()
        {
            var defaultTypes = new List<JobType>
            {
                new JobType
                {
                    Name = "Wedding Photography",
                    Description = "Full wedding day coverage including ceremony and reception",
                    HasPhotos = true,
                    HasLocation = true,
                    HasClientInfo = true,
                    HasPricing = true,
                    HasDueDate = true,
                    HasStatus = true,
                    HasNotes = true,
                    HasUrgentFlag = true,
                    Color = "#FF6B6B",
                    Icon = "💒",
                    StatusOptions = "Consultation,Booked,Pre-Wedding,Wedding Day,Editing,Delivered",
                    CustomFields = System.Text.Json.JsonSerializer.Serialize(new List<CustomField>
                    {
                        new CustomField { Name = "Wedding Date", Type = "date", Required = true },
                        new CustomField { Name = "Ceremony Time", Type = "text", Required = true },
                        new CustomField { Name = "Reception Venue", Type = "text", Required = false },
                        new CustomField { Name = "Package Type", Type = "dropdown", Required = true, Options = "Basic,Standard,Premium,Luxury" },
                        new CustomField { Name = "Second Shooter", Type = "boolean", Required = false, DefaultValue = "false" }
                    })
                },
                new JobType
                {
                    Name = "Product Photography",
                    Description = "E-commerce and commercial product photography",
                    HasPhotos = true,
                    HasLocation = true,
                    HasClientInfo = true,
                    HasPricing = true,
                    HasDueDate = true,
                    HasStatus = true,
                    HasNotes = true,
                    HasUrgentFlag = true,
                    Color = "#4ECDC4",
                    Icon = "📦",
                    StatusOptions = "Scheduled,In Studio,Editing,Review,Approved,Delivered",
                    CustomFields = System.Text.Json.JsonSerializer.Serialize(new List<CustomField>
                    {
                        new CustomField { Name = "Product Category", Type = "dropdown", Required = true, Options = "Electronics,Clothing,Food,Home & Garden,Beauty,Other" },
                        new CustomField { Name = "Number of Products", Type = "number", Required = true },
                        new CustomField { Name = "Background Style", Type = "dropdown", Required = true, Options = "White,Black,Natural,Studio,Outdoor" },
                        new CustomField { Name = "Resolution Required", Type = "dropdown", Required = true, Options = "Standard HD,4K,Print Quality" }
                    })
                },
                new JobType
                {
                    Name = "Real Estate Photography",
                    Description = "Professional property photography for listings",
                    HasPhotos = true,
                    HasLocation = true,
                    HasClientInfo = true,
                    HasPricing = true,
                    HasDueDate = true,
                    HasStatus = true,
                    HasNotes = true,
                    HasUrgentFlag = true,
                    Color = "#45B7D1",
                    Icon = "🏠",
                    StatusOptions = "Scheduled,On Site,Processing,Review,Delivered",
                    CustomFields = System.Text.Json.JsonSerializer.Serialize(new List<CustomField>
                    {
                        new CustomField { Name = "Property Type", Type = "dropdown", Required = true, Options = "House,Condo,Townhouse,Apartment,Commercial,Land" },
                        new CustomField { Name = "Square Footage", Type = "number", Required = false },
                        new CustomField { Name = "Number of Bedrooms", Type = "number", Required = false },
                        new CustomField { Name = "Number of Bathrooms", Type = "number", Required = false },
                        new CustomField { Name = "Shoot Type", Type = "dropdown", Required = true, Options = "Interior Only,Exterior Only,Both,Drone" }
                    })
                },
                new JobType
                {
                    Name = "Portrait Photography",
                    Description = "Individual and family portrait sessions",
                    HasPhotos = true,
                    HasLocation = true,
                    HasClientInfo = true,
                    HasPricing = true,
                    HasDueDate = true,
                    HasStatus = true,
                    HasNotes = true,
                    HasUrgentFlag = true,
                    Color = "#96CEB4",
                    Icon = "👨‍👩‍👧‍👦",
                    StatusOptions = "Scheduled,Session,Editing,Review,Delivered",
                    CustomFields = System.Text.Json.JsonSerializer.Serialize(new List<CustomField>
                    {
                        new CustomField { Name = "Session Type", Type = "dropdown", Required = true, Options = "Individual,Family,Senior,Professional,Engagement,Maternity" },
                        new CustomField { Name = "Number of People", Type = "number", Required = true },
                        new CustomField { Name = "Location Type", Type = "dropdown", Required = true, Options = "Studio,Outdoor,Client Home,Other" },
                        new CustomField { Name = "Outfit Changes", Type = "number", Required = false, DefaultValue = "1" }
                    })
                }
            };

            foreach (var jobType in defaultTypes)
            {
                await _database.InsertAsync(jobType);
            }
        }

        public async Task<List<JobType>> GetJobTypesAsync()
        {
            return await _database.Table<JobType>().OrderBy(x => x.Name).ToListAsync();
        }

        public async Task<JobType?> GetJobTypeAsync(int id)
        {
            return await _database.Table<JobType>().Where(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<int> SaveJobTypeAsync(JobType jobType)
        {
            if (jobType.Id != 0)
            {
                return await _database.UpdateAsync(jobType);
            }
            else
            {
                return await _database.InsertAsync(jobType);
            }
        }

        public async Task<int> DeleteJobTypeAsync(JobType jobType)
        {
            return await _database.DeleteAsync(jobType);
        }

        public async Task<List<JobType>> SearchJobTypesAsync(string searchTerm)
        {
            return await _database.Table<JobType>()
                .Where(x => x.Name.Contains(searchTerm) || x.Description.Contains(searchTerm))
                .OrderBy(x => x.Name)
                .ToListAsync();
        }
    }
} 