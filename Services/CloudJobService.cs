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
        private readonly string? _idToken;

        public CloudJobService(string userId, string? idToken = null)
        {
            _userId = userId;
            _idToken = idToken;
            _httpClient = new HttpClient();
            _baseUrl = $"{FirebaseConfig.DatabaseUrl}/{FirebaseConfig.JobsCollection}/{userId}";
            
            System.Diagnostics.Debug.WriteLine($"CloudJobService: Initialized for user {userId}");
            System.Diagnostics.Debug.WriteLine($"CloudJobService: Base URL: {_baseUrl}");
            Console.WriteLine($"CloudJobService: Initialized for user {userId}");
            Console.WriteLine($"CloudJobService: Base URL: {_baseUrl}");
        }

        private string GetAuthToken()
        {
            // Use ID token if available, otherwise fall back to API key
            return !string.IsNullOrEmpty(_idToken) ? _idToken : FirebaseConfig.ApiKey;
        }

        public async Task<List<PhotoJob>> GetJobsAsync(bool includeSharedJobs = true)
        {
            try
            {
                var allJobs = new List<PhotoJob>();
                
                // Get user's own jobs
                System.Diagnostics.Debug.WriteLine($"CloudJobService: Getting jobs from URL: {_baseUrl}.json");
                Console.WriteLine($"CloudJobService: Getting jobs from URL: {_baseUrl}.json");
                
                var response = await _httpClient.GetAsync($"{_baseUrl}.json?auth={GetAuthToken()}");
                
                System.Diagnostics.Debug.WriteLine($"CloudJobService: Response status: {response.StatusCode}");
                Console.WriteLine($"CloudJobService: Response status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    
                    System.Diagnostics.Debug.WriteLine($"CloudJobService: Raw JSON response: {json}");
                    Console.WriteLine($"CloudJobService: Raw JSON response: {json}");
                    
                    if (!string.IsNullOrEmpty(json) && json != "null")
                    {
                        // Try to deserialize as dictionary first (normal Firebase format)
                        try
                        {
                            var jobsData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(json);
                            if (jobsData != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"CloudJobService: Found {jobsData.Count} jobs in Firebase data (dictionary format)");
                                Console.WriteLine($"CloudJobService: Found {jobsData.Count} jobs in Firebase data (dictionary format)");
                                
                                foreach (var kvp in jobsData)
                                {
                                    try
                                    {
                                        System.Diagnostics.Debug.WriteLine($"CloudJobService: Processing job with key: {kvp.Key}");
                                        Console.WriteLine($"CloudJobService: Processing job with key: {kvp.Key}");
                                        
                                        var job = ConvertFromFirebase(kvp.Value);
                                        job.CloudId = kvp.Key;
                                        
                                        if (int.TryParse(kvp.Key, out int localId))
                                        {
                                            job.Id = localId;
                                        }
                                        else
                                        {
                                            job.Id = 0;
                                        }
                                        
                                        allJobs.Add(job);
                                        System.Diagnostics.Debug.WriteLine($"CloudJobService: Successfully parsed job: {job.Title} (CloudId: {job.CloudId})");
                                        Console.WriteLine($"CloudJobService: Successfully parsed job: {job.Title} (CloudId: {job.CloudId})");
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"CloudJobService: Error parsing job {kvp.Key}: {ex.Message}");
                                        Console.WriteLine($"CloudJobService: Error parsing job {kvp.Key}: {ex.Message}");
                                    }
                                }
                            }
                        }
                        catch (JsonException)
                        {
                            // If dictionary deserialization fails, try array format (happens when jobs are saved with numeric keys)
                            try
                            {
                                var jobsArray = JsonSerializer.Deserialize<JsonElement[]>(json);
                                if (jobsArray != null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"CloudJobService: Found {jobsArray.Length} items in Firebase data (array format)");
                                    Console.WriteLine($"CloudJobService: Found {jobsArray.Length} items in Firebase data (array format)");
                                    
                                    for (int i = 0; i < jobsArray.Length; i++)
                                    {
                                        try
                                        {
                                            var element = jobsArray[i];
                                            
                                            // Skip null entries
                                            if (element.ValueKind == JsonValueKind.Null)
                                            {
                                                continue;
                                            }
                                            
                                            // Convert JsonElement to Dictionary for processing
                                            var jobDict = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());
                                            if (jobDict != null)
                                            {
                                                var cloudId = i.ToString();
                                                System.Diagnostics.Debug.WriteLine($"CloudJobService: Processing job at index {i} (CloudId: {cloudId})");
                                                Console.WriteLine($"CloudJobService: Processing job at index {i} (CloudId: {cloudId})");
                                                
                                                var job = ConvertFromFirebase(jobDict);
                                                job.CloudId = cloudId;
                                                job.Id = i;
                                                
                                                allJobs.Add(job);
                                                System.Diagnostics.Debug.WriteLine($"CloudJobService: Successfully parsed job: {job.Title} (CloudId: {job.CloudId})");
                                                Console.WriteLine($"CloudJobService: Successfully parsed job: {job.Title} (CloudId: {job.CloudId})");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"CloudJobService: Error parsing job at index {i}: {ex.Message}");
                                            Console.WriteLine($"CloudJobService: Error parsing job at index {i}: {ex.Message}");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"CloudJobService: Error deserializing jobs (both dictionary and array formats failed): {ex.Message}");
                                Console.WriteLine($"CloudJobService: Error deserializing jobs (both dictionary and array formats failed): {ex.Message}");
                            }
                        }
                    }
                }

                // Get shared jobs from linked accounts if enabled
                if (includeSharedJobs)
                {
                    var sharedJobs = await GetSharedJobsAsync();
                    allJobs.AddRange(sharedJobs);
                }

                System.Diagnostics.Debug.WriteLine($"CloudJobService: Retrieved {allJobs.Count} total jobs from cloud (including shared)");
                Console.WriteLine($"CloudJobService: Retrieved {allJobs.Count} total jobs from cloud (including shared)");
                return allJobs;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CloudJobService: Error getting jobs: {ex.Message}");
                Console.WriteLine($"CloudJobService: Error getting jobs: {ex.Message}");
                return new List<PhotoJob>();
            }
        }

        private async Task<List<PhotoJob>> GetSharedJobsAsync()
        {
            var sharedJobs = new List<PhotoJob>();
            try
            {
                var accountLinkingService = new AccountLinkingService(_userId);
                var linkedUserIds = await accountLinkingService.GetSharedUserIdsAsync();
                
                foreach (var linkedUserId in linkedUserIds)
                {
                    if (linkedUserId == _userId) continue; // Skip own user (already fetched)
                    
                    var sharedUrl = $"{FirebaseConfig.DatabaseUrl}/sharedJobs/{linkedUserId}/jobs.json?auth={GetAuthToken()}";
                    var response = await _httpClient.GetAsync(sharedUrl);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(json) && json != "null")
                        {
                            var jobsData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(json);
                            if (jobsData != null)
                            {
                                foreach (var kvp in jobsData)
                                {
                                    try
                                    {
                                        var job = ConvertFromFirebase(kvp.Value);
                                        job.CloudId = kvp.Key;
                                        job.UserId = linkedUserId; // Mark as shared from this user
                                        if (int.TryParse(kvp.Key, out int localId))
                                        {
                                            job.Id = localId;
                                        }
                                        sharedJobs.Add(job);
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"CloudJobService: Error parsing shared job {kvp.Key}: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CloudJobService: Error getting shared jobs: {ex.Message}");
            }
            return sharedJobs;
        }

        public async Task<(bool success, string? errorMessage)> SaveJobAsync(PhotoJob job, bool shareWithLinkedAccounts = true)
        {
            try
            {
                var authToken = GetAuthToken();
                if (string.IsNullOrEmpty(authToken))
                {
                    return (false, "No authentication token available. Please sign in again.");
                }

                var data = ConvertToFirebase(job);
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                string requestUrl;
                
                if (job.Id > 0)
                {
                    // Update existing job
                    requestUrl = $"{_baseUrl}/{job.Id}.json?auth={Uri.EscapeDataString(authToken)}";
                    System.Diagnostics.Debug.WriteLine($"CloudJobService: PUT URL: {requestUrl}");
                    response = await _httpClient.PutAsync(requestUrl, content);
                    System.Diagnostics.Debug.WriteLine($"CloudJobService: Updated job {job.Id} in cloud - Status: {response.StatusCode}");
                    Console.WriteLine($"CloudJobService: Updated job {job.Id} in cloud - Status: {response.StatusCode}");
                }
                else
                {
                    // Create new job
                    requestUrl = $"{_baseUrl}.json?auth={Uri.EscapeDataString(authToken)}";
                    System.Diagnostics.Debug.WriteLine($"CloudJobService: POST URL: {requestUrl}");
                    response = await _httpClient.PostAsync(requestUrl, content);
                    
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

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"CloudJobService: Error response: {response.StatusCode} - {errorContent}");
                    Console.WriteLine($"CloudJobService: Error response: {response.StatusCode} - {errorContent}");
                    System.Diagnostics.Debug.WriteLine($"CloudJobService: Request URL was: {requestUrl}");
                    return (false, $"HTTP {response.StatusCode}: {errorContent}");
                }

                // If sharing is enabled, also save to shared folder for linked accounts
                if (shareWithLinkedAccounts)
                {
                    await SaveJobToSharedFolderAsync(job, data);
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error saving job: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"CloudJobService: {errorMsg}");
                Console.WriteLine($"CloudJobService: {errorMsg}");
                System.Diagnostics.Debug.WriteLine($"CloudJobService: Stack trace: {ex.StackTrace}");
                return (false, errorMsg);
            }
        }

        private async Task SaveJobToSharedFolderAsync(PhotoJob job, Dictionary<string, object> data)
        {
            try
            {
                var accountLinkingService = new AccountLinkingService(_userId);
                var linkedUserIds = await accountLinkingService.GetSharedUserIdsAsync();
                
                foreach (var linkedUserId in linkedUserIds)
                {
                    var authToken = GetAuthToken();
                    var sharedUrl = $"{FirebaseConfig.DatabaseUrl}/sharedJobs/{linkedUserId}/jobs/{job.Id}.json?auth={Uri.EscapeDataString(authToken)}";
                    var json = JsonSerializer.Serialize(data);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    var sharedResponse = await _httpClient.PutAsync(sharedUrl, content);
                    if (sharedResponse.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"CloudJobService: Saved job {job.Id} to shared folder for user {linkedUserId}");
                    }
                    else
                    {
                        var errorContent = await sharedResponse.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"CloudJobService: Failed to save to shared folder for {linkedUserId}: {sharedResponse.StatusCode} - {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CloudJobService: Error saving job to shared folder: {ex.Message}");
                // Don't fail the main save if shared folder save fails
            }
        }

        public async Task<bool> DeleteJobAsync(string cloudId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/{cloudId}.json?auth={GetAuthToken()}");
                
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
                ["DueDate"] = job.DueDate?.ToString() ?? string.Empty,
                ["ClientName"] = job.ClientName,
                ["ClientPhone"] = job.ClientPhone,
                ["ClientEmail"] = job.ClientEmail,
                ["Notes"] = job.Notes,
                ["IsUrgent"] = job.IsUrgent,
                ["CreatedDate"] = job.CreatedDate.ToString(),
                ["JobTypeId"] = job.JobTypeId,
                ["Photos"] = job.Photos,
                ["UserId"] = job.UserId,
                ["CustomFieldValues"] = job.CustomFieldValues ?? string.Empty
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

                // Parse DueDate - it might be null or empty
                DateTime? dueDate = null;
                var dueDateStr = GetValue("DueDate", string.Empty);
                if (!string.IsNullOrEmpty(dueDateStr))
                {
                    if (DateTime.TryParse(dueDateStr, out DateTime parsedDate))
                    {
                        dueDate = parsedDate;
                    }
                }

                return new PhotoJob
                {
                    Title = GetValue("Title", string.Empty),
                    Description = GetValue("Description", string.Empty),
                    Status = GetValue("Status", "Pending"),
                    Price = GetValue("Price", 0m),
                    Location = GetValue("Location", string.Empty),
                    DueDate = dueDate,
                    ClientName = GetValue("ClientName", string.Empty),
                    ClientPhone = GetValue("ClientPhone", string.Empty),
                    ClientEmail = GetValue("ClientEmail", string.Empty),
                    Notes = GetValue("Notes", string.Empty),
                    IsUrgent = GetValue("IsUrgent", false),
                    CreatedDate = ParseDateTime("CreatedDate", DateTime.Now),
                    JobTypeId = GetValue("JobTypeId", 0),
                    Photos = GetValue("Photos", string.Empty),
                    UserId = GetValue("UserId", string.Empty),
                    CustomFieldValues = GetValue("CustomFieldValues", string.Empty)
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