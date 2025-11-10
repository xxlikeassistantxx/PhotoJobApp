using PhotoJobApp.Models;
using System.Text.Json;
using System.Text;

namespace PhotoJobApp.Services
{
    public class CloudJobService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _userId;

        public CloudJobService(string userId)
        {
            _userId = userId;
            _httpClient = new HttpClient();
            _baseUrl = $"https://{FirebaseConfig.ProjectId}.firebaseio.com/{FirebaseConfig.JobsCollection}/{userId}";
            
            System.Diagnostics.Debug.WriteLine($"CloudJobService: Initialized for user {userId}");
            System.Diagnostics.Debug.WriteLine($"CloudJobService: Base URL: {_baseUrl}");
            Console.WriteLine($"CloudJobService: Initialized for user {userId}");
            Console.WriteLine($"CloudJobService: Base URL: {_baseUrl}");
        }

        public async Task<List<PhotoJob>> GetJobsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"CloudJobService: Getting jobs from URL: {_baseUrl}.json");
                Console.WriteLine($"CloudJobService: Getting jobs from URL: {_baseUrl}.json");
                
                var response = await _httpClient.GetAsync($"{_baseUrl}.json?auth={FirebaseConfig.ApiKey}");
                
                System.Diagnostics.Debug.WriteLine($"CloudJobService: Response status: {response.StatusCode}");
                Console.WriteLine($"CloudJobService: Response status: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"CloudJobService: Failed to get jobs. Status: {response.StatusCode}");
                    Console.WriteLine($"CloudJobService: Failed to get jobs. Status: {response.StatusCode}");
                    return new List<PhotoJob>();
                }

                var json = await response.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"CloudJobService: Raw JSON response: {json}");
                Console.WriteLine($"CloudJobService: Raw JSON response: {json}");
                
                if (string.IsNullOrEmpty(json) || json == "null")
                {
                    System.Diagnostics.Debug.WriteLine("CloudJobService: No jobs found in cloud");
                    Console.WriteLine("CloudJobService: No jobs found in cloud");
                    return new List<PhotoJob>();
                }

                var jobsData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(json);
                var jobs = new List<PhotoJob>();

                if (jobsData != null)
                {
                    System.Diagnostics.Debug.WriteLine($"CloudJobService: Found {jobsData.Count} jobs in Firebase data");
                    Console.WriteLine($"CloudJobService: Found {jobsData.Count} jobs in Firebase data");
                    
                    foreach (var kvp in jobsData)
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"CloudJobService: Processing job with key: {kvp.Key}");
                            Console.WriteLine($"CloudJobService: Processing job with key: {kvp.Key}");
                            
                            var job = ConvertFromFirebase(kvp.Value);
                            job.CloudId = kvp.Key; // Set the CloudId to the Firebase key
                            
                            // Try to parse the key as an integer for the local ID, but don't fail if it's not
                            if (int.TryParse(kvp.Key, out int localId))
                            {
                                job.Id = localId;
                            }
                            else
                            {
                                job.Id = 0; // Use 0 for cloud-only jobs
                            }
                            
                            jobs.Add(job);
                            System.Diagnostics.Debug.WriteLine($"CloudJobService: Successfully parsed job: {job.Title} (CloudId: {job.CloudId})");
                            Console.WriteLine($"CloudJobService: Successfully parsed job: {job.Title} (CloudId: {job.CloudId})");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"CloudJobService: Error parsing job {kvp.Key}: {ex.Message}");
                            Console.WriteLine($"CloudJobService: Error parsing job {kvp.Key}: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"CloudJobService: Stack trace: {ex.StackTrace}");
                            Console.WriteLine($"CloudJobService: Stack trace: {ex.StackTrace}");
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"CloudJobService: Retrieved {jobs.Count} jobs from cloud");
                Console.WriteLine($"CloudJobService: Retrieved {jobs.Count} jobs from cloud");
                return jobs;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CloudJobService: Error getting jobs: {ex.Message}");
                Console.WriteLine($"CloudJobService: Error getting jobs: {ex.Message}");
                return new List<PhotoJob>();
            }
        }

        public async Task<bool> SaveJobAsync(PhotoJob job)
        {
            try
            {
                var data = ConvertToFirebase(job);
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                
                if (job.Id > 0)
                {
                    // Update existing job
                    response = await _httpClient.PutAsync($"{_baseUrl}/{job.Id}.json?auth={FirebaseConfig.ApiKey}", content);
                    System.Diagnostics.Debug.WriteLine($"CloudJobService: Updated job {job.Id} in cloud");
                    Console.WriteLine($"CloudJobService: Updated job {job.Id} in cloud");
                }
                else
                {
                    // Create new job
                    response = await _httpClient.PostAsync($"{_baseUrl}.json?auth={FirebaseConfig.ApiKey}", content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<Dictionary<string, string>>(responseJson);
                        if (result != null && result.ContainsKey("name"))
                        {
                            job.Id = int.Parse(result["name"]);
                            System.Diagnostics.Debug.WriteLine($"CloudJobService: Created job {job.Id} in cloud");
                            Console.WriteLine($"CloudJobService: Created job {job.Id} in cloud");
                        }
                    }
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CloudJobService: Error saving job: {ex.Message}");
                Console.WriteLine($"CloudJobService: Error saving job: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteJobAsync(string cloudId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/{cloudId}.json?auth={FirebaseConfig.ApiKey}");
                
                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"CloudJobService: Deleted job {cloudId} from cloud");
                    Console.WriteLine($"CloudJobService: Deleted job {cloudId} from cloud");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"CloudJobService: Failed to delete job {cloudId}. Status: {response.StatusCode}");
                    Console.WriteLine($"CloudJobService: Failed to delete job {cloudId}. Status: {response.StatusCode}");
                }
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CloudJobService: Error deleting job: {ex.Message}");
                Console.WriteLine($"CloudJobService: Error deleting job: {ex.Message}");
                return false;
            }
        }

        private Dictionary<string, object> ConvertToFirebase(PhotoJob job)
        {
            return new Dictionary<string, object>
            {
                ["Title"] = job.Title,
                ["Description"] = job.Description,
                ["Status"] = job.Status,
                ["Price"] = job.Price,
                ["Location"] = job.Location,
                ["DueDate"] = job.DueDate.ToString(),
                ["ClientName"] = job.ClientName,
                ["ClientPhone"] = job.ClientPhone,
                ["ClientEmail"] = job.ClientEmail,
                ["Notes"] = job.Notes,
                ["IsUrgent"] = job.IsUrgent,
                ["CreatedDate"] = job.CreatedDate.ToString(),
                ["JobTypeId"] = job.JobTypeId,
                ["Photos"] = job.Photos,
                ["UserId"] = job.UserId
            };
        }

        private PhotoJob ConvertFromFirebase(Dictionary<string, object> data)
        {
            try
            {
                // Helper function to safely convert values
                T GetValue<T>(string key, T defaultValue)
                {
                    if (!data.ContainsKey(key)) return defaultValue;
                    var value = data[key];
                    if (value is T typedValue) return typedValue;
                    
                    try
                    {
                        // Handle specific type conversions
                        if (typeof(T) == typeof(string))
                        {
                            return (T)(object)value.ToString();
                        }
                        else if (typeof(T) == typeof(int))
                        {
                            if (value is JsonElement element && element.ValueKind == JsonValueKind.Number)
                            {
                                return (T)(object)element.GetInt32();
                            }
                            return (T)(object)Convert.ToInt32(value);
                        }
                        else if (typeof(T) == typeof(decimal))
                        {
                            if (value is JsonElement element && element.ValueKind == JsonValueKind.Number)
                            {
                                return (T)(object)element.GetDecimal();
                            }
                            return (T)(object)Convert.ToDecimal(value);
                        }
                        else if (typeof(T) == typeof(bool))
                        {
                            if (value is JsonElement element && element.ValueKind == JsonValueKind.True)
                            {
                                return (T)(object)true;
                            }
                            else if (value is JsonElement element2 && element2.ValueKind == JsonValueKind.False)
                            {
                                return (T)(object)false;
                            }
                            return (T)(object)Convert.ToBoolean(value);
                        }
                        else
                        {
                            return (T)Convert.ChangeType(value, typeof(T));
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"CloudJobService: Error converting {key} from {value?.GetType()} to {typeof(T)}: {ex.Message}");
                        Console.WriteLine($"CloudJobService: Error converting {key} from {value?.GetType()} to {typeof(T)}: {ex.Message}");
                        return defaultValue;
                    }
                }

                // Helper function to safely parse DateTime
                DateTime ParseDateTime(string key, DateTime defaultValue)
                {
                    if (!data.ContainsKey(key)) return defaultValue;
                    var value = data[key];
                    if (value is DateTime dt) return dt;
                    
                    try
                    {
                        if (value is JsonElement element && element.ValueKind == JsonValueKind.String)
                        {
                            return DateTime.Parse(element.GetString());
                        }
                        return DateTime.Parse(value.ToString());
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"CloudJobService: Error parsing DateTime for {key}: {ex.Message}");
                        Console.WriteLine($"CloudJobService: Error parsing DateTime for {key}: {ex.Message}");
                        return defaultValue;
                    }
                }

                return new PhotoJob
                {
                    Title = GetValue("Title", string.Empty),
                    Description = GetValue("Description", string.Empty),
                    Status = GetValue("Status", "Pending"),
                    Price = GetValue("Price", 0m),
                    Location = GetValue("Location", string.Empty),
                    DueDate = ParseDateTime("DueDate", DateTime.Now),
                    ClientName = GetValue("ClientName", string.Empty),
                    ClientPhone = GetValue("ClientPhone", string.Empty),
                    ClientEmail = GetValue("ClientEmail", string.Empty),
                    Notes = GetValue("Notes", string.Empty),
                    IsUrgent = GetValue("IsUrgent", false),
                    CreatedDate = ParseDateTime("CreatedDate", DateTime.Now),
                    JobTypeId = GetValue("JobTypeId", 0),
                    Photos = GetValue("Photos", string.Empty),
                    UserId = GetValue("UserId", string.Empty)
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CloudJobService: Error in ConvertFromFirebase: {ex.Message}");
                Console.WriteLine($"CloudJobService: Error in ConvertFromFirebase: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"CloudJobService: Stack trace: {ex.StackTrace}");
                Console.WriteLine($"CloudJobService: Stack trace: {ex.StackTrace}");
                
                // Return a default job with basic info
                return new PhotoJob
                {
                    Title = "Error Loading Job",
                    Description = "Failed to load job data from cloud",
                    Status = "Error",
                    CreatedDate = DateTime.Now
                };
            }
        }
    }
} 