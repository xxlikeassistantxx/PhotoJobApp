using SQLite;
using PhotoJobApp.Models;
using System.Collections.ObjectModel;

namespace PhotoJobApp.Services
{
    public class PhotoJobService
    {
        private readonly SQLiteAsyncConnection _database;
        private readonly string _databasePath;

        public PhotoJobService()
        {
            _databasePath = Path.Combine(FileSystem.AppDataDirectory, "PhotoJobs.db3");
            _database = new SQLiteAsyncConnection(_databasePath);
            InitializeDatabaseAsync().Wait();
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                await _database.CreateTableAsync<PhotoJob>();
                
                // Add some sample data if the table is empty
                var existingJobs = await _database.Table<PhotoJob>().CountAsync();
                if (existingJobs == 0)
                {
                    await AddSampleDataAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
            }
        }

        private async Task AddSampleDataAsync()
        {
            var sampleJobs = new List<PhotoJob>
            {
                new PhotoJob
                {
                    Title = "Wedding Photography",
                    Description = "Full day wedding coverage including ceremony and reception",
                    ClientName = "John & Sarah Smith",
                    ClientPhone = "555-0123",
                    ClientEmail = "john.sarah@email.com",
                    Price = 2500.00m,
                    Status = "In Progress",
                    CreatedDate = DateTime.Now.AddDays(-5),
                    DueDate = DateTime.Now.AddDays(10),
                    Location = "Grand Hotel, Downtown",
                    Notes = "Bride prefers natural lighting, outdoor ceremony",
                    IsUrgent = false
                },
                new PhotoJob
                {
                    Title = "Product Photography",
                    Description = "E-commerce product photos for online store",
                    ClientName = "TechGear Inc",
                    ClientPhone = "555-0456",
                    ClientEmail = "marketing@techgear.com",
                    Price = 800.00m,
                    Status = "Completed",
                    CreatedDate = DateTime.Now.AddDays(-10),
                    CompletedDate = DateTime.Now.AddDays(-2),
                    Location = "Studio A",
                    Notes = "White background, high resolution for web",
                    IsUrgent = false
                },
                new PhotoJob
                {
                    Title = "Real Estate Photography",
                    Description = "Professional photos for property listing",
                    ClientName = "City Real Estate",
                    ClientPhone = "555-0789",
                    ClientEmail = "listings@cityreal.com",
                    Price = 350.00m,
                    Status = "Pending",
                    CreatedDate = DateTime.Now.AddDays(-1),
                    DueDate = DateTime.Now.AddDays(3),
                    Location = "123 Main Street",
                    Notes = "Include both interior and exterior shots",
                    IsUrgent = true
                }
            };

            foreach (var job in sampleJobs)
            {
                await _database.InsertAsync(job);
            }
        }

        public async Task<List<PhotoJob>> GetJobsAsync()
        {
            return await _database.Table<PhotoJob>().OrderByDescending(x => x.CreatedDate).ToListAsync();
        }

        public async Task<PhotoJob> GetJobAsync(int id)
        {
            return await _database.Table<PhotoJob>().Where(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<int> SaveJobAsync(PhotoJob job)
        {
            if (job.Id != 0)
            {
                return await _database.UpdateAsync(job);
            }
            else
            {
                return await _database.InsertAsync(job);
            }
        }

        public async Task<int> DeleteJobAsync(PhotoJob job)
        {
            return await _database.DeleteAsync(job);
        }

        public async Task<List<PhotoJob>> GetJobsByStatusAsync(string status)
        {
            return await _database.Table<PhotoJob>().Where(x => x.Status == status).OrderByDescending(x => x.CreatedDate).ToListAsync();
        }

        public async Task<List<PhotoJob>> SearchJobsAsync(string searchTerm)
        {
            return await _database.Table<PhotoJob>()
                .Where(x => x.Title.Contains(searchTerm) || 
                           x.ClientName.Contains(searchTerm) || 
                           x.Description.Contains(searchTerm))
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<PhotoJob>> GetUrgentJobsAsync()
        {
            return await _database.Table<PhotoJob>()
                .Where(x => x.IsUrgent && x.Status != "Completed")
                .OrderBy(x => x.DueDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            var completedJobs = await _database.Table<PhotoJob>().Where(x => x.Status == "Completed").ToListAsync();
            return completedJobs.Sum(x => x.Price);
        }

        public async Task<int> GetJobsCountByStatusAsync(string status)
        {
            return await _database.Table<PhotoJob>().Where(x => x.Status == status).CountAsync();
        }
    }
}
