using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhotoJobApp.Services
{
    public class FirebaseAuthService
    {
        private const string FIREBASE_API_KEY = "AIzaSyDYCKj1mp7GrEftKYPMnoXYrt6EwNsje6c";
        private const string FIREBASE_AUTH_URL = "https://identitytoolkit.googleapis.com/v1/accounts";
        private readonly HttpClient _httpClient;

        public FirebaseAuthService()
        {
            _httpClient = new HttpClient();
        }

        public class FirebaseUser
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string IdToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
        }

        public class FirebaseAuthResponse
        {
            [JsonPropertyName("idToken")]
            public string? IdToken { get; set; }
            
            [JsonPropertyName("refreshToken")]
            public string? RefreshToken { get; set; }
            
            [JsonPropertyName("localId")]
            public string? LocalId { get; set; }
            
            [JsonPropertyName("email")]
            public string? Email { get; set; }
            
            [JsonPropertyName("displayName")]
            public string? DisplayName { get; set; }
            
            [JsonPropertyName("error")]
            public FirebaseAuthError? Error { get; set; }
        }

        public class FirebaseAuthError
        {
            public int Code { get; set; }
            public string? Message { get; set; }
        }

        public async Task<(bool success, FirebaseUser? user, string? error)> SignUpAsync(string email, string password, string displayName = "")
        {
            try
            {
                var requestData = new
                {
                    email = email,
                    password = password,
                    returnSecureToken = true
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{FIREBASE_AUTH_URL}:signUp?key={FIREBASE_API_KEY}";
                System.Diagnostics.Debug.WriteLine($"Firebase SignUp URL: {url}");
                System.Diagnostics.Debug.WriteLine($"Firebase SignUp Request: {json}");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"Firebase SignUp Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Firebase SignUp Response: {responseContent}");
                
                var authResponse = JsonSerializer.Deserialize<FirebaseAuthResponse>(responseContent);

                if (authResponse?.Error != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Firebase SignUp Error: {authResponse.Error.Message}");
                    return (false, null, GetErrorMessage(authResponse.Error.Message ?? "Unknown error"));
                }

                if (authResponse?.IdToken != null && authResponse?.LocalId != null)
                {
                    var user = new FirebaseUser
                    {
                        Id = authResponse.LocalId,
                        Email = authResponse.Email ?? email,
                        DisplayName = displayName,
                        IdToken = authResponse.IdToken,
                        RefreshToken = authResponse.RefreshToken ?? string.Empty
                    };

                    // Store authentication data
                    _ = StoreAuthData(user);

                    // Send email verification
                    await SendEmailVerificationAsync(authResponse.IdToken);

                    return (true, user, null);
                }

                return (false, null, "Sign up failed. Please try again.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Firebase SignUp Exception: {ex.Message}");
                return (false, null, $"Sign up failed: {ex.Message}");
            }
        }

        public async Task<(bool success, FirebaseUser? user, string? error)> SignInAsync(string email, string password)
        {
            try
            {
                var requestData = new
                {
                    email = email,
                    password = password,
                    returnSecureToken = true
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{FIREBASE_AUTH_URL}:signInWithPassword?key={FIREBASE_API_KEY}";
                System.Diagnostics.Debug.WriteLine($"Firebase SignIn URL: {url}");
                System.Diagnostics.Debug.WriteLine($"Firebase SignIn Request: {json}");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"Firebase SignIn Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Firebase SignIn Response: {responseContent}");
                
                var authResponse = JsonSerializer.Deserialize<FirebaseAuthResponse>(responseContent);

                if (authResponse?.Error != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Firebase SignIn Error: {authResponse.Error.Message}");
                    return (false, null, GetErrorMessage(authResponse.Error.Message ?? "Unknown error"));
                }

                if (authResponse?.IdToken != null && authResponse?.LocalId != null)
                {
                    var user = new FirebaseUser
                    {
                        Id = authResponse.LocalId,
                        Email = authResponse.Email ?? email,
                        DisplayName = authResponse.DisplayName ?? string.Empty,
                        IdToken = authResponse.IdToken,
                        RefreshToken = authResponse.RefreshToken ?? string.Empty
                    };

                    // Store authentication data
                    _ = StoreAuthData(user);

                    return (true, user, null);
                }

                return (false, null, "Unknown error occurred during sign in");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Firebase SignIn Exception: {ex.Message}");
                return (false, null, $"Network error: {ex.Message}");
            }
        }

        public Task<bool> SignOutAsync()
        {
            try
            {
                // Clear stored authentication data
                Preferences.Remove("FirebaseUserId");
                Preferences.Remove("FirebaseUserEmail");
                Preferences.Remove("FirebaseUserDisplayName");
                Preferences.Remove("FirebaseIdToken");
                Preferences.Remove("FirebaseRefreshToken");
                Preferences.Remove("IsAuthenticated");

                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public Task<FirebaseUser?> GetCurrentUserAsync()
        {
            try
            {
                var userId = Preferences.Get("FirebaseUserId", string.Empty);
                var email = Preferences.Get("FirebaseUserEmail", string.Empty);
                var displayName = Preferences.Get("FirebaseUserDisplayName", string.Empty);
                var idToken = Preferences.Get("FirebaseIdToken", string.Empty);
                var refreshToken = Preferences.Get("FirebaseRefreshToken", string.Empty);

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(idToken))
                {
                    return Task.FromResult<FirebaseUser?>(null);
                }

                // Check if token is still valid (you might want to add token validation here)
                return Task.FromResult<FirebaseUser?>(new FirebaseUser
                {
                    Id = userId,
                    Email = email,
                    DisplayName = displayName,
                    IdToken = idToken,
                    RefreshToken = refreshToken
                });
            }
            catch
            {
                return Task.FromResult<FirebaseUser?>(null);
            }
        }

        public bool IsAuthenticated()
        {
            return Preferences.Get("IsAuthenticated", false);
        }

        public async Task<string?> GetIdTokenAsync()
        {
            var user = await GetCurrentUserAsync();
            return user?.IdToken;
        }

        private Task StoreAuthData(FirebaseUser user)
        {
            Preferences.Set("FirebaseUserId", user.Id);
            Preferences.Set("FirebaseUserEmail", user.Email);
            Preferences.Set("FirebaseUserDisplayName", user.DisplayName);
            Preferences.Set("FirebaseIdToken", user.IdToken);
            Preferences.Set("FirebaseRefreshToken", user.RefreshToken);
            Preferences.Set("IsAuthenticated", true);
            return Task.CompletedTask;
        }

        private string GetErrorMessage(string errorCode)
        {
            return errorCode switch
            {
                "EMAIL_EXISTS" => "An account with this email already exists.",
                "EMAIL_NOT_FOUND" => "No account found with this email address.",
                "INVALID_PASSWORD" => "Incorrect password.",
                "WEAK_PASSWORD" => "Password should be at least 6 characters long.",
                "INVALID_EMAIL" => "Please enter a valid email address.",
                "TOO_MANY_ATTEMPTS_TRY_LATER" => "Too many failed attempts. Please try again later.",
                "USER_DISABLED" => "This account has been disabled.",
                "MISSING_EMAIL" => "Email address is required.",
                "INVALID_REQUEST_TYPE" => "Invalid request type.",
                _ => $"Authentication error: {errorCode}"
            };
        }

        public async Task<bool> SendEmailVerificationAsync(string idToken)
        {
            try
            {
                var requestData = new
                {
                    requestType = "VERIFY_EMAIL",
                    idToken = idToken
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{FIREBASE_AUTH_URL}:sendOobCode?key={FIREBASE_API_KEY}";
                System.Diagnostics.Debug.WriteLine($"Firebase SendEmailVerification URL: {url}");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"Firebase SendEmailVerification Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Firebase SendEmailVerification Response: {responseContent}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Firebase SendEmailVerification Exception: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsEmailVerifiedAsync(string idToken)
        {
            try
            {
                var requestData = new
                {
                    idToken = idToken
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{FIREBASE_AUTH_URL}:lookup?key={FIREBASE_API_KEY}";
                System.Diagnostics.Debug.WriteLine($"Firebase IsEmailVerified URL: {url}");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"Firebase IsEmailVerified Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Firebase IsEmailVerified Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var lookupResponse = JsonSerializer.Deserialize<FirebaseLookupResponse>(responseContent);
                    return lookupResponse?.Users?.FirstOrDefault()?.EmailVerified ?? false;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Firebase IsEmailVerified Exception: {ex.Message}");
                return false;
            }
        }

        public class FirebaseLookupResponse
        {
            [JsonPropertyName("users")]
            public List<FirebaseUserInfo>? Users { get; set; }
        }

        public class FirebaseUserInfo
        {
            [JsonPropertyName("localId")]
            public string? LocalId { get; set; }
            
            [JsonPropertyName("email")]
            public string? Email { get; set; }
            
            [JsonPropertyName("emailVerified")]
            public bool EmailVerified { get; set; }
        }

        public async Task<(bool success, string? error)> SendPasswordResetEmailAsync(string email)
        {
            try
            {
                var requestData = new
                {
                    requestType = "PASSWORD_RESET",
                    email = email
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{FIREBASE_AUTH_URL}:sendOobCode?key={FIREBASE_API_KEY}";
                System.Diagnostics.Debug.WriteLine($"Firebase SendPasswordResetEmail URL: {url}");
                System.Diagnostics.Debug.WriteLine($"Firebase SendPasswordResetEmail Request: {json}");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"Firebase SendPasswordResetEmail Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Firebase SendPasswordResetEmail Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }

                // Parse error response if available
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<FirebaseAuthResponse>(responseContent);
                    if (errorResponse?.Error != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Firebase SendPasswordResetEmail Error: {errorResponse.Error.Message}");
                        return (false, GetErrorMessage(errorResponse.Error.Message ?? "Unknown error"));
                    }
                }
                catch
                {
                    // Ignore parsing errors for error responses
                }

                return (false, "Failed to send password reset email. Please try again.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Firebase SendPasswordResetEmail Exception: {ex.Message}");
                return (false, $"Network error: {ex.Message}");
            }
        }
    }
} 