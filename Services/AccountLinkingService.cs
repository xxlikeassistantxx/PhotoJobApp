using System.Text.Json;
using System.Text;

namespace PhotoJobApp.Services
{
    public class AccountLinkingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _userId;

        public AccountLinkingService(string userId)
        {
            _userId = userId;
            _httpClient = new HttpClient();
            _baseUrl = $"{FirebaseConfig.DatabaseUrl}/linkedAccounts/{userId}";
        }

        /// <summary>
        /// Link another account to the current user's account
        /// </summary>
        public async Task<bool> LinkAccountAsync(string linkedUserId, string linkedUserEmail)
        {
            try
            {
                var linkedAccount = new
                {
                    UserId = linkedUserId,
                    Email = linkedUserEmail,
                    LinkedDate = DateTime.UtcNow.ToString("O")
                };

                var json = JsonSerializer.Serialize(linkedAccount);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(
                    $"{_baseUrl}/linkedUserIds/{linkedUserId}.json?auth={FirebaseConfig.ApiKey}", 
                    content);

                if (response.IsSuccessStatusCode)
                {
                    // Also add reverse link (bidirectional)
                    var reverseBaseUrl = $"{FirebaseConfig.DatabaseUrl}/linkedAccounts/{linkedUserId}";
                    var currentUser = await GetCurrentUserInfoAsync();
                    if (currentUser != null)
                    {
                        var reverseLinkedAccount = new
                        {
                            UserId = _userId,
                            Email = currentUser.Email,
                            LinkedDate = DateTime.UtcNow.ToString("O")
                        };
                        var reverseJson = JsonSerializer.Serialize(reverseLinkedAccount);
                        var reverseContent = new StringContent(reverseJson, Encoding.UTF8, "application/json");
                        await _httpClient.PutAsync(
                            $"{reverseBaseUrl}/linkedUserIds/{_userId}.json?auth={FirebaseConfig.ApiKey}",
                            reverseContent);
                    }
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AccountLinkingService: Error linking account: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unlink an account from the current user's account
        /// </summary>
        public async Task<bool> UnlinkAccountAsync(string linkedUserId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(
                    $"{_baseUrl}/linkedUserIds/{linkedUserId}.json?auth={FirebaseConfig.ApiKey}");

                if (response.IsSuccessStatusCode)
                {
                    // Also remove reverse link
                    var reverseBaseUrl = $"https://{FirebaseConfig.ProjectId}.firebaseio.com/linkedAccounts/{linkedUserId}";
                    await _httpClient.DeleteAsync(
                        $"{reverseBaseUrl}/linkedUserIds/{_userId}.json?auth={FirebaseConfig.ApiKey}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AccountLinkingService: Error unlinking account: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get all linked account IDs for the current user
        /// </summary>
        public async Task<List<LinkedAccountInfo>> GetLinkedAccountsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{_baseUrl}/linkedUserIds.json?auth={FirebaseConfig.ApiKey}");

                if (!response.IsSuccessStatusCode)
                {
                    return new List<LinkedAccountInfo>();
                }

                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(json) || json == "null")
                {
                    return new List<LinkedAccountInfo>();
                }

                var linkedAccountsData = JsonSerializer.Deserialize<Dictionary<string, LinkedAccountInfo>>(json);
                if (linkedAccountsData == null)
                {
                    return new List<LinkedAccountInfo>();
                }

                return linkedAccountsData.Values.ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AccountLinkingService: Error getting linked accounts: {ex.Message}");
                return new List<LinkedAccountInfo>();
            }
        }

        /// <summary>
        /// Get all user IDs that should have access to shared data (current user + linked users)
        /// </summary>
        public async Task<List<string>> GetSharedUserIdsAsync()
        {
            var userIds = new List<string> { _userId };
            var linkedAccounts = await GetLinkedAccountsAsync();
            userIds.AddRange(linkedAccounts.Select(la => la.UserId));
            return userIds.Distinct().ToList();
        }

        private async Task<FirebaseUserInfo?> GetCurrentUserInfoAsync()
        {
            try
            {
                var authService = new FirebaseAuthService();
                var currentUser = await authService.GetCurrentUserAsync();
                if (currentUser != null)
                {
                    return new FirebaseUserInfo
                    {
                        Id = currentUser.Id,
                        Email = currentUser.Email
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AccountLinkingService: Error getting current user info: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Find a user by email address (for linking)
        /// Creates a user record if it doesn't exist, using email hash as ID
        /// </summary>
        public async Task<string?> FindUserIdByEmailAsync(string email)
        {
            try
            {
                // Create a deterministic user ID from email (hash-based)
                // In production, you'd want to use Firebase Auth's user lookup
                var emailHash = email.GetHashCode().ToString("X");
                var userId = $"user_{emailHash}";
                
                // Check if user record exists, create if not
                var usersUrl = $"{FirebaseConfig.DatabaseUrl}/users/{userId}.json?auth={FirebaseConfig.ApiKey}";
                var response = await _httpClient.GetAsync(usersUrl);
                
                if (!response.IsSuccessStatusCode || await response.Content.ReadAsStringAsync() == "null")
                {
                    // Create user record
                    var userData = new
                    {
                        Email = email,
                        CreatedDate = DateTime.UtcNow.ToString("O")
                    };
                    var json = JsonSerializer.Serialize(userData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    await _httpClient.PutAsync(usersUrl, content);
                }
                
                return userId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AccountLinkingService: Error finding/creating user by email: {ex.Message}");
                return null;
            }
        }
    }

    public class LinkedAccountInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string LinkedDate { get; set; } = string.Empty;
    }

    public class FirebaseUserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}

