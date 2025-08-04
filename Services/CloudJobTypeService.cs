using PhotoJobApp.Models;
using System.Text.Json;
using System.Text;

namespace PhotoJobApp.Services
{
    public class CloudJobTypeService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _userId;

        public CloudJobTypeService(string userId)
        {
            _userId = userId;
            _httpClient = new HttpClient();
            _baseUrl = $"https://{FirebaseConfig.ProjectId}.firebaseio.com/{FirebaseConfig.JobTypesCollection}/{userId}";
            
            System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Initialized for user {userId}");
            System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Base URL: {_baseUrl}");
            Console.WriteLine($"CloudJobTypeService: Initialized for user {userId}");
            Console.WriteLine($"CloudJobTypeService: Base URL: {_baseUrl}");
        }

        public async Task<List<JobType>> GetJobTypesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Getting job types from URL: {_baseUrl}.json");
                Console.WriteLine($"CloudJobTypeService: Getting job types from URL: {_baseUrl}.json");
                
                var response = await _httpClient.GetAsync($"{_baseUrl}.json?auth={FirebaseConfig.ApiKey}");
                
                System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Response status: {response.StatusCode}");
                Console.WriteLine($"CloudJobTypeService: Response status: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Failed to get job types. Status: {response.StatusCode}");
                    Console.WriteLine($"CloudJobTypeService: Failed to get job types. Status: {response.StatusCode}");
                    return new List<JobType>();
                }

                var json = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Response JSON: {json}");
                Console.WriteLine($"CloudJobTypeService: Response JSON: {json}");
                
                if (string.IsNullOrEmpty(json) || json == "null")
                {
                    System.Diagnostics.Debug.WriteLine("CloudJobTypeService: No job types found in cloud");
                    Console.WriteLine("CloudJobTypeService: No job types found in cloud");
                    return new List<JobType>();
                }

                var jobTypesData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(json);
                var jobTypes = new List<JobType>();

                if (jobTypesData != null)
                {
                    System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Found {jobTypesData.Count} job types in cloud data");
                    Console.WriteLine($"CloudJobTypeService: Found {jobTypesData.Count} job types in cloud data");
                    
                    foreach (var kvp in jobTypesData)
                    {
                        try
                        {
                            var jobType = ConvertFromFirebase(kvp.Value);
                            jobType.CloudId = kvp.Key; // Set the CloudId to the Firebase key
                            jobTypes.Add(jobType);
                            System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Parsed job type: {jobType.Name} (CloudId: {jobType.CloudId})");
                            Console.WriteLine($"CloudJobTypeService: Parsed job type: {jobType.Name} (CloudId: {jobType.CloudId})");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Error parsing job type {kvp.Key}: {ex.Message}");
                            Console.WriteLine($"CloudJobTypeService: Error parsing job type {kvp.Key}: {ex.Message}");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("CloudJobTypeService: jobTypesData is null");
                    Console.WriteLine("CloudJobTypeService: jobTypesData is null");
                }

                System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Retrieved {jobTypes.Count} job types from cloud");
                Console.WriteLine($"CloudJobTypeService: Retrieved {jobTypes.Count} job types from cloud");
                return jobTypes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Error getting job types: {ex.Message}");
                Console.WriteLine($"CloudJobTypeService: Error getting job types: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Stack trace: {ex.StackTrace}");
                Console.WriteLine($"CloudJobTypeService: Stack trace: {ex.StackTrace}");
                return new List<JobType>();
            }
        }

        public async Task<bool> SaveJobTypeAsync(JobType jobType)
        {
            try
            {
                var data = ConvertToFirebase(jobType);
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                
                if (!string.IsNullOrEmpty(jobType.CloudId))
                {
                    // Update existing job type
                    response = await _httpClient.PutAsync($"{_baseUrl}/{jobType.CloudId}.json?auth={FirebaseConfig.ApiKey}", content);
                    System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Updated job type {jobType.Name} (CloudId: {jobType.CloudId}) in cloud");
                    Console.WriteLine($"CloudJobTypeService: Updated job type {jobType.Name} (CloudId: {jobType.CloudId}) in cloud");
                }
                else
                {
                    // Create new job type
                    response = await _httpClient.PostAsync($"{_baseUrl}.json?auth={FirebaseConfig.ApiKey}", content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<Dictionary<string, string>>(responseJson);
                        if (result != null && result.ContainsKey("name"))
                        {
                            jobType.CloudId = result["name"]; // Firebase generates a unique ID
                            System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Created job type {jobType.Name} with CloudId: {jobType.CloudId} in cloud");
                            Console.WriteLine($"CloudJobTypeService: Created job type {jobType.Name} with CloudId: {jobType.CloudId} in cloud");
                        }
                    }
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Error saving job type: {ex.Message}");
                Console.WriteLine($"CloudJobTypeService: Error saving job type: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteJobTypeAsync(string cloudId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/{cloudId}.json?auth={FirebaseConfig.ApiKey}");
                
                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Deleted job type {cloudId} from cloud");
                    Console.WriteLine($"CloudJobTypeService: Deleted job type {cloudId} from cloud");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Failed to delete job type {cloudId}. Status: {response.StatusCode}");
                    Console.WriteLine($"CloudJobTypeService: Failed to delete job type {cloudId}. Status: {response.StatusCode}");
                }
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Error deleting job type: {ex.Message}");
                Console.WriteLine($"CloudJobTypeService: Error deleting job type: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SyncJobTypesAsync(List<JobType> localJobTypes)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Starting sync with {localJobTypes.Count} local job types");
                Console.WriteLine($"CloudJobTypeService: Starting sync with {localJobTypes.Count} local job types");
                
                var cloudJobTypes = await GetJobTypesAsync();
                System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Retrieved {cloudJobTypes.Count} job types from cloud");
                Console.WriteLine($"CloudJobTypeService: Retrieved {cloudJobTypes.Count} job types from cloud");
                
                // Find job types that exist in cloud but not locally (download them)
                var cloudOnlyJobTypes = cloudJobTypes.Where(ct => !localJobTypes.Any(lt => lt.CloudId == ct.CloudId)).ToList();
                
                // Find job types that exist locally but not in cloud (upload them)
                var localOnlyJobTypes = localJobTypes.Where(lt => !cloudJobTypes.Any(ct => ct.CloudId == lt.CloudId)).ToList();
                
                // Find job types that exist in both but have different timestamps (resolve conflicts)
                var conflictingJobTypes = localJobTypes.Where(lt => 
                    cloudJobTypes.Any(ct => ct.CloudId == lt.CloudId && ct.CreatedDate != lt.CreatedDate)).ToList();

                System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Sync found {cloudOnlyJobTypes.Count} cloud-only, {localOnlyJobTypes.Count} local-only, {conflictingJobTypes.Count} conflicting");
                Console.WriteLine($"CloudJobTypeService: Sync found {cloudOnlyJobTypes.Count} cloud-only, {localOnlyJobTypes.Count} local-only, {conflictingJobTypes.Count} conflicting");

                // Log local job type CloudIds
                var localCloudIds = string.Join(", ", localJobTypes.Select(lt => lt.CloudId ?? "null"));
                System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Local job type CloudIds: [{localCloudIds}]");
                Console.WriteLine($"CloudJobTypeService: Local job type CloudIds: [{localCloudIds}]");
                
                // Log cloud job type CloudIds
                var cloudCloudIds = string.Join(", ", cloudJobTypes.Select(ct => ct.CloudId ?? "null"));
                System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Cloud job type CloudIds: [{cloudCloudIds}]");
                Console.WriteLine($"CloudJobTypeService: Cloud job type CloudIds: [{cloudCloudIds}]");

                // For now, we'll use a simple strategy: local wins for conflicts
                // In a real app, you'd want more sophisticated conflict resolution
                foreach (var jobType in localOnlyJobTypes.Concat(conflictingJobTypes))
                {
                    System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Uploading job type to cloud: {jobType.Name} (CloudId: {jobType.CloudId ?? "new"})");
                    Console.WriteLine($"CloudJobTypeService: Uploading job type to cloud: {jobType.Name} (CloudId: {jobType.CloudId ?? "new"})");
                    var success = await SaveJobTypeAsync(jobType);
                    System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Upload result for {jobType.Name}: {success}");
                    Console.WriteLine($"CloudJobTypeService: Upload result for {jobType.Name}: {success}");
                }

                System.Diagnostics.Debug.WriteLine("CloudJobTypeService: Sync completed successfully");
                Console.WriteLine("CloudJobTypeService: Sync completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Error syncing job types: {ex.Message}");
                Console.WriteLine($"CloudJobTypeService: Error syncing job types: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Sync stack trace: {ex.StackTrace}");
                Console.WriteLine($"CloudJobTypeService: Sync stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private Dictionary<string, object> ConvertToFirebase(JobType jobType)
        {
            return new Dictionary<string, object>
            {
                ["Name"] = jobType.Name,
                ["Description"] = jobType.Description,
                ["HasPhotos"] = jobType.HasPhotos,
                ["HasLocation"] = jobType.HasLocation,
                ["HasClientInfo"] = jobType.HasClientInfo,
                ["HasPricing"] = jobType.HasPricing,
                ["HasDueDate"] = jobType.HasDueDate,
                ["HasStatus"] = jobType.HasStatus,
                ["HasNotes"] = jobType.HasNotes,
                ["HasUrgentFlag"] = jobType.HasUrgentFlag,
                ["Color"] = jobType.Color,
                ["Icon"] = jobType.Icon,
                ["CreatedDate"] = jobType.CreatedDate.ToString("O"),
                ["UserId"] = jobType.UserId,
                ["CustomFields"] = jobType.CustomFields,
                ["StatusOptions"] = jobType.StatusOptions,
                ["CloudId"] = jobType.CloudId
            };
        }

        private JobType ConvertFromFirebase(Dictionary<string, object> data)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Converting Firebase data: {System.Text.Json.JsonSerializer.Serialize(data)}");
                Console.WriteLine($"CloudJobTypeService: Converting Firebase data: {System.Text.Json.JsonSerializer.Serialize(data)}");
                
                // Helper function to safely convert JsonElement to bool
                bool GetBoolValue(string key, bool defaultValue = false)
                {
                    if (!data.ContainsKey(key)) return defaultValue;
                    var value = data[key];
                    if (value is System.Text.Json.JsonElement jsonElement)
                    {
                        return jsonElement.GetBoolean();
                    }
                    return Convert.ToBoolean(value);
                }
                
                return new JobType
                {
                    Name = data.ContainsKey("Name") ? data["Name"]?.ToString() ?? string.Empty : string.Empty,
                    Description = data.ContainsKey("Description") ? data["Description"]?.ToString() ?? string.Empty : string.Empty,
                    HasPhotos = GetBoolValue("HasPhotos"),
                    HasLocation = GetBoolValue("HasLocation"),
                    HasClientInfo = GetBoolValue("HasClientInfo"),
                    HasPricing = GetBoolValue("HasPricing"),
                    HasDueDate = GetBoolValue("HasDueDate"),
                    HasStatus = GetBoolValue("HasStatus"),
                    HasNotes = GetBoolValue("HasNotes"),
                    HasUrgentFlag = GetBoolValue("HasUrgentFlag"),
                    Color = data.ContainsKey("Color") ? data["Color"]?.ToString() ?? "#512BD4" : "#512BD4",
                    Icon = data.ContainsKey("Icon") ? data["Icon"]?.ToString() ?? "📷" : "📷",
                    CreatedDate = data.ContainsKey("CreatedDate") ? DateTime.Parse(data["CreatedDate"]?.ToString() ?? DateTime.Now.ToString()) : DateTime.Now,
                    UserId = data.ContainsKey("UserId") ? data["UserId"]?.ToString() ?? string.Empty : string.Empty,
                    CustomFields = data.ContainsKey("CustomFields") ? data["CustomFields"]?.ToString() ?? string.Empty : string.Empty,
                    StatusOptions = data.ContainsKey("StatusOptions") ? data["StatusOptions"]?.ToString() ?? "Pending,In Progress,Completed,Cancelled" : "Pending,In Progress,Completed,Cancelled",
                    CloudId = data.ContainsKey("CloudId") ? data["CloudId"]?.ToString() : null
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Error converting Firebase data: {ex.Message}");
                Console.WriteLine($"CloudJobTypeService: Error converting Firebase data: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"CloudJobTypeService: Error stack trace: {ex.StackTrace}");
                Console.WriteLine($"CloudJobTypeService: Error stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
} 