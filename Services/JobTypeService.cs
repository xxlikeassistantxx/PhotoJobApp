using SQLite;
using PhotoJobApp.Models;
using System.Collections.ObjectModel;

namespace PhotoJobApp.Services
{
    public class JobTypeService
    {
        private readonly SQLiteAsyncConnection _database;
        private readonly string _databasePath;
        private CloudJobTypeService _cloudService;
        private string _userId;
        
        public bool IsCloudSyncAvailable => !string.IsNullOrEmpty(_userId);

        private JobTypeService()
        {
            _databasePath = Path.Combine(FileSystem.AppDataDirectory, "PhotoJobs.db3");
            System.Diagnostics.Debug.WriteLine($"JobTypeService: Database path: {_databasePath}");
            Console.WriteLine($"JobTypeService: Database path: {_databasePath}");
            _database = new SQLiteAsyncConnection(_databasePath);
        }

        public static async Task<JobTypeService> CreateAsync(string userId = null)
        {
            var service = new JobTypeService();
            service._userId = userId;
            if (!string.IsNullOrEmpty(userId))
            {
                service._cloudService = new CloudJobTypeService(userId);
            }
            await service.InitializeDatabaseAsync();
            return service;
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                await _database.CreateTableAsync<JobType>();
                
                // Add some default job types if the table is empty
                var existingTypes = await _database.Table<JobType>().CountAsync();
                System.Diagnostics.Debug.WriteLine($"JobTypeService: Found {existingTypes} existing job types in database");
                Console.WriteLine($"JobTypeService: Found {existingTypes} existing job types in database");
                
                if (existingTypes == 0)
                {
                    System.Diagnostics.Debug.WriteLine("JobTypeService: Adding default job types");
                    Console.WriteLine("JobTypeService: Adding default job types");
                    await AddDefaultJobTypesAsync();
                    
                    // Verify the data was added
                    var typesAfterAdd = await _database.Table<JobType>().CountAsync();
                    System.Diagnostics.Debug.WriteLine($"JobTypeService: After adding default types, found {typesAfterAdd} job types");
                    Console.WriteLine($"JobTypeService: After adding default types, found {typesAfterAdd} job types");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("JobTypeService: Skipping default job types - types already exist");
                    Console.WriteLine("JobTypeService: Skipping default job types - types already exist");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JobType database initialization error: {ex.Message}");
            }
        }

        private Task AddDefaultJobTypesAsync()
        {
            // No default job types - users will create their own
            System.Diagnostics.Debug.WriteLine("JobTypeService: No default job types added - users will create their own");
            Console.WriteLine("JobTypeService: No default job types added - users will create their own");
            return Task.CompletedTask;
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
            int result;
            if (jobType.Id != 0)
            {
                result = await _database.UpdateAsync(jobType);
            }
            else
            {
                result = await _database.InsertAsync(jobType);
            }

            // Sync to cloud if available
            if (result > 0 && _cloudService != null)
            {
                _ = Task.Run(async () => await SaveJobTypeToCloudAsync(jobType));
            }

            return result;
        }

        public async Task<int> DeleteJobTypeAsync(JobType jobType)
        {
            var cloudId = jobType.CloudId;
            var result = await _database.DeleteAsync(jobType);

            // Delete from cloud if available
            if (result > 0 && _cloudService != null && !string.IsNullOrEmpty(cloudId))
            {
                _ = Task.Run(async () => await DeleteJobTypeFromCloudAsync(cloudId));
            }

            return result;
        }

        public async Task<int> ClearAllJobTypesAsync()
        {
            System.Diagnostics.Debug.WriteLine("JobTypeService: Clearing all job types from database");
            Console.WriteLine("JobTypeService: Clearing all job types from database");
            
            var result = await _database.DeleteAllAsync<JobType>();
            
            System.Diagnostics.Debug.WriteLine($"JobTypeService: Cleared {result} job types from database");
            Console.WriteLine($"JobTypeService: Cleared {result} job types from database");
            
            return result;
        }

        public async Task<List<JobType>> SearchJobTypesAsync(string searchTerm)
        {
            return await _database.Table<JobType>()
                .Where(x => x.Name.Contains(searchTerm) || x.Description.Contains(searchTerm))
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        // Cloud sync methods
        public async Task<bool> SyncWithCloudAsync()
        {
            try
            {
                if (_cloudService == null)
                {
                    System.Diagnostics.Debug.WriteLine("JobTypeService: Cloud service not available for sync");
                    Console.WriteLine("JobTypeService: Cloud service not available for sync");
                    return false;
                }

                var localJobTypes = await GetJobTypesAsync();
                var success = await _cloudService.SyncJobTypesAsync(localJobTypes);
                
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine("JobTypeService: Cloud sync completed successfully");
                    Console.WriteLine("JobTypeService: Cloud sync completed successfully");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("JobTypeService: Cloud sync failed");
                    Console.WriteLine("JobTypeService: Cloud sync failed");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JobTypeService: Error during cloud sync: {ex.Message}");
                Console.WriteLine($"JobTypeService: Error during cloud sync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SaveJobTypeToCloudAsync(JobType jobType)
        {
            try
            {
                if (_cloudService == null)
                {
                    System.Diagnostics.Debug.WriteLine("JobTypeService: Cloud service not available");
                    Console.WriteLine("JobTypeService: Cloud service not available");
                    return false;
                }

                var success = await _cloudService.SaveJobTypeAsync(jobType);
                
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"JobTypeService: Job type {jobType.Id} saved to cloud successfully");
                    Console.WriteLine($"JobTypeService: Job type {jobType.Id} saved to cloud successfully");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"JobTypeService: Failed to save job type {jobType.Id} to cloud");
                    Console.WriteLine($"JobTypeService: Failed to save job type {jobType.Id} to cloud");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JobTypeService: Error saving job type to cloud: {ex.Message}");
                Console.WriteLine($"JobTypeService: Error saving job type to cloud: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteJobTypeFromCloudAsync(string cloudId)
        {
            try
            {
                if (_cloudService == null)
                {
                    System.Diagnostics.Debug.WriteLine("JobTypeService: Cloud service not available");
                    Console.WriteLine("JobTypeService: Cloud service not available");
                    return false;
                }

                var success = await _cloudService.DeleteJobTypeAsync(cloudId);
                
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"JobTypeService: Job type {cloudId} deleted from cloud successfully");
                    Console.WriteLine($"JobTypeService: Job type {cloudId} deleted from cloud successfully");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"JobTypeService: Failed to delete job type {cloudId} from cloud");
                    Console.WriteLine($"JobTypeService: Failed to delete job type {cloudId} from cloud");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JobTypeService: Error deleting job type from cloud: {ex.Message}");
                Console.WriteLine($"JobTypeService: Error deleting job type from cloud: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PullFromCloudAsync()
        {
            try
            {
                if (_cloudService == null)
                {
                    System.Diagnostics.Debug.WriteLine("JobTypeService: Cloud service not available for pull");
                    Console.WriteLine("JobTypeService: Cloud service not available for pull");
                    return false;
                }

                var cloudJobTypes = await _cloudService.GetJobTypesAsync();
                System.Diagnostics.Debug.WriteLine($"JobTypeService: Retrieved {cloudJobTypes.Count} job types from cloud");
                Console.WriteLine($"JobTypeService: Retrieved {cloudJobTypes.Count} job types from cloud");

                var localJobTypes = await GetJobTypesAsync();
                var newJobTypes = new List<JobType>();

                foreach (var cloudJobType in cloudJobTypes)
                {
                    // Check if this job type already exists locally by CloudId
                    var existingLocal = localJobTypes.FirstOrDefault(lt => lt.CloudId == cloudJobType.CloudId);
                    if (existingLocal == null)
                    {
                        // This is a new job type from cloud, add it locally
                        cloudJobType.Id = 0; // Reset ID so it gets a new local ID
                        await _database.InsertAsync(cloudJobType);
                        newJobTypes.Add(cloudJobType);
                        System.Diagnostics.Debug.WriteLine($"JobTypeService: Added new job type from cloud: {cloudJobType.Name} (CloudId: {cloudJobType.CloudId})");
                        Console.WriteLine($"JobTypeService: Added new job type from cloud: {cloudJobType.Name} (CloudId: {cloudJobType.CloudId})");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"JobTypeService: Job type already exists locally: {cloudJobType.Name} (CloudId: {cloudJobType.CloudId})");
                        Console.WriteLine($"JobTypeService: Job type already exists locally: {cloudJobType.Name} (CloudId: {cloudJobType.CloudId})");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"JobTypeService: Pull completed. Added {newJobTypes.Count} new job types from cloud");
                Console.WriteLine($"JobTypeService: Pull completed. Added {newJobTypes.Count} new job types from cloud");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JobTypeService: Error pulling from cloud: {ex.Message}");
                Console.WriteLine($"JobTypeService: Error pulling from cloud: {ex.Message}");
                return false;
            }
        }
    }
} 