using SQLite;
using PhotoJobApp.Models;
using System.Collections.ObjectModel;

namespace PhotoJobApp.Services
{
    public class PhotoJobService
    {
        private readonly SQLiteAsyncConnection _database;
        private readonly string _databasePath;
        private bool _isInitialized = false;

        public PhotoJobService()
        {
            _databasePath = Path.Combine(FileSystem.AppDataDirectory, "PhotoJobs.db3");
            System.Diagnostics.Debug.WriteLine($"PhotoJobService: Database path: {_databasePath}");
            Console.WriteLine($"PhotoJobService: Database path: {_databasePath}");
            _database = new SQLiteAsyncConnection(_databasePath);
            // Don't block the constructor - initialize asynchronously
            _ = InitializeDatabaseAsync();
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("PhotoJobService: Starting database initialization");
                Console.WriteLine("PhotoJobService: Starting database initialization");
                
                await _database.CreateTableAsync<PhotoJob>();
                
                System.Diagnostics.Debug.WriteLine("PhotoJobService: Database table created");
                Console.WriteLine("PhotoJobService: Database table created");
                
                // No sample data - users will create their own jobs
                var existingJobs = await _database.Table<PhotoJob>().CountAsync();
                System.Diagnostics.Debug.WriteLine($"PhotoJobService: Found {existingJobs} existing jobs in database");
                Console.WriteLine($"PhotoJobService: Found {existingJobs} existing jobs in database");
                
                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("PhotoJobService: Database initialization completed");
                Console.WriteLine("PhotoJobService: Database initialization completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
                Console.WriteLine($"Database initialization error: {ex.Message}");
            }
        }



        public async Task<List<PhotoJob>> GetJobsAsync()
        {
            // Wait for initialization to complete if needed
            while (!_isInitialized)
            {
                await Task.Delay(10);
            }
            return await _database.Table<PhotoJob>().OrderByDescending(x => x.CreatedDate).ToListAsync();
        }

        public async Task<PhotoJob> GetJobAsync(int id)
        {
            // Wait for initialization to complete if needed
            while (!_isInitialized)
            {
                await Task.Delay(10);
            }
            return await _database.GetAsync<PhotoJob>(id);
        }

        public async Task<PhotoJob> GetJobByIdAsync(int id)
        {
            // Wait for initialization to complete if needed
            while (!_isInitialized)
            {
                await Task.Delay(10);
            }
            return await _database.GetAsync<PhotoJob>(id);
        }

        public async Task<int> SaveJobAsync(PhotoJob job)
        {
            // Wait for initialization to complete if needed
            while (!_isInitialized)
            {
                await Task.Delay(10);
            }
            
            if (job.Id == 0)
            {
                return await _database.InsertAsync(job);
            }
            else
            {
                return await _database.UpdateAsync(job);
            }
        }

        public async Task<int> DeleteJobAsync(PhotoJob job)
        {
            // Wait for initialization to complete if needed
            while (!_isInitialized)
            {
                await Task.Delay(10);
            }
            return await _database.DeleteAsync(job);
        }

        public async Task<int> ClearAllJobsAsync()
        {
            // Wait for initialization to complete if needed
            while (!_isInitialized)
            {
                await Task.Delay(10);
            }
            
            System.Diagnostics.Debug.WriteLine("PhotoJobService: Clearing all jobs from database");
            Console.WriteLine("PhotoJobService: Clearing all jobs from database");
            
            var result = await _database.DeleteAllAsync<PhotoJob>();
            
            System.Diagnostics.Debug.WriteLine($"PhotoJobService: Cleared {result} jobs from database");
            Console.WriteLine($"PhotoJobService: Cleared {result} jobs from database");
            
            return result;
        }

        public async Task<List<PhotoJob>> GetJobsByStatusAsync(string status)
        {
            // Wait for initialization to complete if needed
            while (!_isInitialized)
            {
                await Task.Delay(10);
            }
            return await _database.Table<PhotoJob>().Where(x => x.Status == status).ToListAsync();
        }

        public async Task<List<PhotoJob>> SearchJobsAsync(string searchTerm)
        {
            // Wait for initialization to complete if needed
            while (!_isInitialized)
            {
                await Task.Delay(10);
            }
            
            return await _database.Table<PhotoJob>()
                .Where(x => x.Title.Contains(searchTerm) || 
                           x.ClientName.Contains(searchTerm) || 
                           x.Description.Contains(searchTerm))
                .ToListAsync();
        }

        public async Task<List<PhotoJob>> GetUrgentJobsAsync()
        {
            // Wait for initialization to complete if needed
            while (!_isInitialized)
            {
                await Task.Delay(10);
            }
            return await _database.Table<PhotoJob>().Where(x => x.IsUrgent).ToListAsync();
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            // Wait for initialization to complete if needed
            while (!_isInitialized)
            {
                await Task.Delay(10);
            }
            var completedJobs = await _database.Table<PhotoJob>().Where(x => x.Status == "Completed").ToListAsync();
            return completedJobs.Sum(x => x.Price);
        }

        public async Task<int> GetJobsCountByStatusAsync(string status)
        {
            // Wait for initialization to complete if needed
            while (!_isInitialized)
            {
                await Task.Delay(10);
            }
            return await _database.Table<PhotoJob>().Where(x => x.Status == status).CountAsync();
        }
    }
}
