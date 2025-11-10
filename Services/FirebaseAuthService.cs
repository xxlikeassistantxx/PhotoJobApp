using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
#if IOS
using Foundation;
#endif

namespace PhotoJobApp.Services
{
    public class FirebaseAuthService
    {
        private const string FIREBASE_API_KEY = "AIzaSyDYCKj1mp7GrEftKYPMnoXYrt6EwNsje6c";
        private const string FIREBASE_AUTH_URL = "https://identitytoolkit.googleapis.com/v1/accounts";
        private readonly HttpClient _httpClient;
        private readonly IConfiguration? _configuration;
#if IOS
        private readonly IGoogleSignInService? _googleSignInService;
#endif
        private const string AuthDataStorageKey = "FirebaseAuthData:v1";
        private static readonly JsonSerializerOptions AuthDataSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public FirebaseAuthService(IConfiguration? configuration = null
#if IOS
            , IGoogleSignInService? googleSignInService = null
#endif
            )
        {
            _httpClient = new HttpClient();
            _configuration = configuration;
#if IOS
            _googleSignInService = googleSignInService;
#endif
        }

        public class FirebaseUser
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string IdToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
        }

#if ANDROID
        private static string GetAndroidPackageName()
        {
            try
            {
                var packageName = Microsoft.Maui.ApplicationModel.AppInfo.Current.PackageName;
                if (!string.IsNullOrWhiteSpace(packageName))
                {
                    return packageName;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FirebaseAuthService: Failed to resolve Android package name via AppInfo: {ex.Message}");
                Console.WriteLine($"FirebaseAuthService: Failed to resolve Android package name via AppInfo: {ex.Message}");
            }

            return "com.pinebelttrophy.photojobapp2025";
        }
#endif

        private class StoredAuthData
        {
            public string UserId { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string IdToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
            public DateTimeOffset LastPersistedUtc { get; set; } = DateTimeOffset.UtcNow;
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
                    await StoreAuthDataAsync(user);

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
                    await StoreAuthDataAsync(user);

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
                try
                {
                    SecureStorage.Remove(AuthDataStorageKey);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SecureStorage.Remove failed: {ex.Message}");
                    Console.WriteLine($"SecureStorage.Remove failed: {ex.Message}");
                }

                Preferences.Remove(AuthDataStorageKey);
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

        public async Task<FirebaseUser?> GetCurrentUserAsync()
        {
            try
            {
                var stored = await LoadStoredAuthDataAsync();

                if (stored == null || string.IsNullOrEmpty(stored.UserId) || string.IsNullOrEmpty(stored.IdToken))
                {
                    return null;
                }

                return new FirebaseUser
                {
                    Id = stored.UserId,
                    Email = stored.Email,
                    DisplayName = stored.DisplayName,
                    IdToken = stored.IdToken,
                    RefreshToken = stored.RefreshToken
                };
            }
            catch
            {
                return null;
            }
        }

        public bool IsAuthenticated()
        {
            if (Preferences.Get("IsAuthenticated", false))
                return true;

            try
            {
                var securePayload = SecureStorage.GetAsync(AuthDataStorageKey).GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(securePayload))
                {
                    Preferences.Set(AuthDataStorageKey, securePayload);
                    Preferences.Set("IsAuthenticated", true);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SecureStorage.GetAsync in IsAuthenticated failed: {ex.Message}");
                Console.WriteLine($"SecureStorage.GetAsync in IsAuthenticated failed: {ex.Message}");
            }

            var cachedPayload = Preferences.Get(AuthDataStorageKey, string.Empty);
            return !string.IsNullOrEmpty(cachedPayload);
        }

        public async Task<string?> GetIdTokenAsync()
        {
            var user = await GetCurrentUserAsync();
            return user?.IdToken;
        }

        /// <summary>
        /// Checks Firebase auth state by verifying if a stored token is still valid.
        /// This is used as a fallback when OAuth callback URL doesn't reach the app.
        /// Implements AuthStateListener pattern by checking and refreshing tokens as needed.
        /// </summary>
        public async Task<(bool isAuthenticated, FirebaseUser? user)> CheckAuthStateAsync()
        {
            try
            {
                // First check local storage
                var localUser = await GetCurrentUserAsync();
                if (localUser != null && !string.IsNullOrEmpty(localUser.IdToken))
                {
                    // Verify token is still valid by calling Firebase's getUserInfo API
                    try
                    {
                        var url = $"{FIREBASE_AUTH_URL}:lookup?key={FIREBASE_API_KEY}";
                        var requestData = new { idToken = localUser.IdToken };
                        var json = JsonSerializer.Serialize(requestData);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var response = await _httpClient.PostAsync(url, content);
                        var responseContent = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            // Token is valid, user is authenticated
                            System.Diagnostics.Debug.WriteLine("Firebase auth state check: Token is valid");
                            Console.WriteLine("Firebase auth state check: Token is valid");
                            return (true, localUser);
                        }
                        else
                        {
                            // Token might be expired, try to refresh it
                            System.Diagnostics.Debug.WriteLine("Firebase auth state check: Token is invalid, attempting refresh...");
                            Console.WriteLine("Firebase auth state check: Token is invalid, attempting refresh...");
                            
                            if (!string.IsNullOrEmpty(localUser.RefreshToken))
                            {
                                var refreshedUser = await RefreshIdTokenAsync(localUser.RefreshToken);
                                if (refreshedUser != null)
                                {
                                    System.Diagnostics.Debug.WriteLine("Firebase auth state check: Token refreshed successfully");
                                    Console.WriteLine("Firebase auth state check: Token refreshed successfully");
                                    return (true, refreshedUser);
                                }
                            }
                            
                            // Token refresh failed, clear local storage
                            System.Diagnostics.Debug.WriteLine("Firebase auth state check: Token refresh failed, clearing local storage");
                            Console.WriteLine("Firebase auth state check: Token refresh failed, clearing local storage");
                            await SignOutAsync();
                            return (false, null);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error verifying token: {ex.Message}");
                        Console.WriteLine($"Error verifying token: {ex.Message}");
                        // If we can't verify, assume local state is correct (offline mode)
                        return (localUser != null, localUser);
                    }
                }

                return (false, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking auth state: {ex.Message}");
                Console.WriteLine($"Error checking auth state: {ex.Message}");
                return (false, null);
            }
        }

        /// <summary>
        /// Refreshes an expired Firebase ID token using the refresh token.
        /// This implements Firebase's token refresh mechanism for session persistence.
        /// </summary>
        public async Task<FirebaseUser?> RefreshIdTokenAsync(string refreshToken)
        {
            try
            {
                if (string.IsNullOrEmpty(refreshToken))
                {
                    System.Diagnostics.Debug.WriteLine("RefreshIdTokenAsync: Refresh token is null or empty");
                    Console.WriteLine("RefreshIdTokenAsync: Refresh token is null or empty");
                    return null;
                }

                var requestData = new
                {
                    grant_type = "refresh_token",
                    refresh_token = refreshToken
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://securetoken.googleapis.com/v1/token?key={FIREBASE_API_KEY}";
                System.Diagnostics.Debug.WriteLine($"Firebase RefreshIdToken URL: {url}");
                System.Diagnostics.Debug.WriteLine($"Firebase RefreshIdToken Request: {json}");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"Firebase RefreshIdToken Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Firebase RefreshIdToken Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var refreshResponse = JsonSerializer.Deserialize<FirebaseRefreshResponse>(responseContent);
                    if (refreshResponse != null && !string.IsNullOrEmpty(refreshResponse.IdToken))
                    {
                        // Get existing user data to preserve email and display name
                        var existingUser = await GetCurrentUserAsync();
                        
                        var refreshedUser = new FirebaseUser
                        {
                            Id = refreshResponse.UserId ?? existingUser?.Id ?? string.Empty,
                            Email = existingUser?.Email ?? string.Empty,
                            DisplayName = existingUser?.DisplayName ?? string.Empty,
                            IdToken = refreshResponse.IdToken,
                            RefreshToken = refreshResponse.RefreshToken ?? refreshToken // Use new refresh token if provided
                        };

                        // Store refreshed auth data
                        await StoreAuthDataAsync(refreshedUser);

                        System.Diagnostics.Debug.WriteLine("Firebase RefreshIdToken: Token refreshed successfully");
                        Console.WriteLine("Firebase RefreshIdToken: Token refreshed successfully");
                        return refreshedUser;
                    }
                }
                else
                {
                    // Parse error response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<FirebaseAuthResponse>(responseContent);
                        if (errorResponse?.Error != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Firebase RefreshIdToken Error: {errorResponse.Error.Message}");
                            Console.WriteLine($"Firebase RefreshIdToken Error: {errorResponse.Error.Message}");
                        }
                    }
                    catch
                    {
                        // Ignore parsing errors
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Firebase RefreshIdToken Exception: {ex.Message}");
                Console.WriteLine($"Firebase RefreshIdToken Exception: {ex.Message}");
                return null;
            }
        }

        public class FirebaseRefreshResponse
        {
            [JsonPropertyName("id_token")]
            public string? IdToken { get; set; }
            
            [JsonPropertyName("refresh_token")]
            public string? RefreshToken { get; set; }
            
            [JsonPropertyName("user_id")]
            public string? UserId { get; set; }
            
            [JsonPropertyName("error")]
            public FirebaseAuthError? Error { get; set; }
        }

        private async Task StoreAuthDataAsync(FirebaseUser user)
        {
            var stored = new StoredAuthData
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName ?? string.Empty,
                IdToken = user.IdToken ?? string.Empty,
                RefreshToken = user.RefreshToken ?? string.Empty,
                LastPersistedUtc = DateTimeOffset.UtcNow
            };

            var payload = JsonSerializer.Serialize(stored, AuthDataSerializerOptions);

            // Persist to SecureStorage first (best effort).
            try
            {
                await SecureStorage.SetAsync(AuthDataStorageKey, payload);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SecureStorage.SetAsync failed: {ex.Message}");
                Console.WriteLine($"SecureStorage.SetAsync failed: {ex.Message}");
            }

            // Always keep a Preferences fallback so legacy flows keep working.
            Preferences.Set(AuthDataStorageKey, payload);

            // Maintain legacy preference keys for backwards compatibility (migration path).
            Preferences.Set("FirebaseUserId", stored.UserId);
            Preferences.Set("FirebaseUserEmail", stored.Email);
            Preferences.Set("FirebaseUserDisplayName", stored.DisplayName);
            Preferences.Set("FirebaseIdToken", stored.IdToken);
            Preferences.Set("FirebaseRefreshToken", stored.RefreshToken);
            Preferences.Set("IsAuthenticated", true);
        }

        private async Task<StoredAuthData?> LoadStoredAuthDataAsync()
        {
            string? payload = null;

            try
            {
                payload = await SecureStorage.GetAsync(AuthDataStorageKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SecureStorage.GetAsync failed: {ex.Message}");
                Console.WriteLine($"SecureStorage.GetAsync failed: {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(payload))
            {
                payload = Preferences.Get(AuthDataStorageKey, string.Empty);
            }

            if (!string.IsNullOrWhiteSpace(payload))
            {
                try
                {
                    return JsonSerializer.Deserialize<StoredAuthData>(payload, AuthDataSerializerOptions);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to deserialize stored auth data: {ex.Message}");
                    Console.WriteLine($"Failed to deserialize stored auth data: {ex.Message}");
                }
            }

            // Legacy preference keys fallback (migration path).
            var legacyUserId = Preferences.Get("FirebaseUserId", string.Empty);
            var legacyEmail = Preferences.Get("FirebaseUserEmail", string.Empty);
            var legacyDisplayName = Preferences.Get("FirebaseUserDisplayName", string.Empty);
            var legacyIdToken = Preferences.Get("FirebaseIdToken", string.Empty);
            var legacyRefreshToken = Preferences.Get("FirebaseRefreshToken", string.Empty);

            if (!string.IsNullOrEmpty(legacyUserId) && !string.IsNullOrEmpty(legacyIdToken))
            {
                var legacyUser = new FirebaseUser
                {
                    Id = legacyUserId,
                    Email = legacyEmail,
                    DisplayName = legacyDisplayName,
                    IdToken = legacyIdToken,
                    RefreshToken = legacyRefreshToken
                };

                // Migrate into secure storage for future loads.
                await StoreAuthDataAsync(legacyUser);

                return new StoredAuthData
                {
                    UserId = legacyUserId,
                    Email = legacyEmail,
                    DisplayName = legacyDisplayName,
                    IdToken = legacyIdToken,
                    RefreshToken = legacyRefreshToken,
                    LastPersistedUtc = DateTimeOffset.UtcNow
                };
            }

            return null;
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

        public async Task<(bool success, FirebaseUser? user, string? error)> SignInWithGoogleAsync()
        {
            try
            {
#if IOS
                // Try to get GoogleSignInService from DI if not already set
                var googleSignInService = _googleSignInService;
                if (googleSignInService == null)
                {
                    try
                    {
                        // Try to get from DI container if available
                        var handler = Application.Current?.Handler;
                        if (handler?.MauiContext != null)
                        {
                            googleSignInService = handler.MauiContext.Services.GetService<IGoogleSignInService>();
                            if (googleSignInService != null)
                            {
                                System.Diagnostics.Debug.WriteLine("FirebaseAuthService: Retrieved GoogleSignInService from DI container");
                                Console.WriteLine("FirebaseAuthService: Retrieved GoogleSignInService from DI container");
                            }
                        }
                    }
                    catch (Exception diEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"FirebaseAuthService: Error getting GoogleSignInService from DI: {diEx.Message}");
                        Console.WriteLine($"FirebaseAuthService: Error getting GoogleSignInService from DI: {diEx.Message}");
                    }
                }
                
                // On iOS, try to use native Google Sign-In SDK first (if available)
                // This handles OAuth flows reliably, including terminated apps during 2FA
                System.Diagnostics.Debug.WriteLine($"FirebaseAuthService: Checking native Google Sign-In service availability...");
                System.Diagnostics.Debug.WriteLine($"FirebaseAuthService: googleSignInService is {(googleSignInService == null ? "NULL" : "AVAILABLE")}");
                Console.WriteLine($"FirebaseAuthService: Checking native Google Sign-In service availability...");
                Console.WriteLine($"FirebaseAuthService: googleSignInService is {(googleSignInService == null ? "NULL" : "AVAILABLE")}");
                
                if (googleSignInService != null)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("FirebaseAuthService: Using native Google Sign-In SDK");
                        Console.WriteLine("FirebaseAuthService: Using native Google Sign-In SDK");
                        
                        var idToken = await googleSignInService.SignInAsync();
                        if (!string.IsNullOrEmpty(idToken))
                        {
                            System.Diagnostics.Debug.WriteLine("FirebaseAuthService: Received ID token from native Google Sign-In");
                            Console.WriteLine("FirebaseAuthService: Received ID token from native Google Sign-In");
                            return await SignInWithGoogleIdTokenAsync(idToken);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("FirebaseAuthService: Native Google Sign-In returned null token, falling back to WebAuthenticator");
                            Console.WriteLine("FirebaseAuthService: Native Google Sign-In returned null token, falling back to WebAuthenticator");
                        }
                    }
                    catch (Exception nativeEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"FirebaseAuthService: Native Google Sign-In error, falling back to WebAuthenticator: {nativeEx.Message}");
                        Console.WriteLine($"FirebaseAuthService: Native Google Sign-In error, falling back to WebAuthenticator: {nativeEx.Message}");
                    }
                }
#endif
                
                // Fallback: Google OAuth flow using WebAuthenticator
                // We'll use Google OAuth directly with the appropriate Client ID
                // (iOS Client ID on iOS, Web Client ID elsewhere)
                // then exchange the ID token with Firebase
                
                // Get the appropriate Google Client ID for this platform
                var googleClientId = GetGoogleClientId();
                
                if (string.IsNullOrEmpty(googleClientId))
                {
                    return (false, null, "Google Client ID is not configured. Please add it to appsettings.json under Firebase:GoogleiOSClientId (iOS) or Firebase:GoogleWebClientId");
                }
                
                System.Diagnostics.Debug.WriteLine($"Using Google Client ID: {googleClientId.Substring(0, Math.Min(30, googleClientId.Length))}...");
                
                string authUrl;
                string callbackUrl;
#if IOS
                var reversedClientId = GetGoogleReversedClientId();
                if (string.IsNullOrEmpty(reversedClientId))
                {
                    return (false, null, "Google reversed client ID is not configured. Ensure GoogleService-Info.plist includes REVERSED_CLIENT_ID.");
                }

                var redirectUri = $"{reversedClientId}:/oauth2redirect";
                var state = Guid.NewGuid().ToString("N");
                var nonce = Guid.NewGuid().ToString("N");
                var scope = Uri.EscapeDataString("openid email profile");

                authUrl =
                    "https://accounts.google.com/o/oauth2/v2/auth?" +
                    $"client_id={Uri.EscapeDataString(googleClientId)}&" +
                    $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                    $"response_type=code%20id_token&" +
                    $"scope={scope}&" +
                    $"state={state}&" +
                    $"nonce={nonce}&" +
                    "access_type=offline&" +
                    "include_granted_scopes=true&" +
                    "prompt=select_account";

                callbackUrl = redirectUri;

                System.Diagnostics.Debug.WriteLine("⚠️ USING DIRECT GOOGLE OAUTH (iOS)");
                Console.WriteLine("⚠️ USING DIRECT GOOGLE OAUTH (iOS)");
                System.Diagnostics.Debug.WriteLine($"  Redirect URI: {redirectUri}");
                System.Diagnostics.Debug.WriteLine($"  State: {state}");
                System.Diagnostics.Debug.WriteLine($"  Nonce: {nonce}");
                System.Diagnostics.Debug.WriteLine($"  OAuth URL (truncated): {authUrl.Substring(0, Math.Min(200, authUrl.Length))}...");
                Console.WriteLine($"  Redirect URI: {redirectUri}");
                Console.WriteLine($"  State: {state}");
                Console.WriteLine($"  Nonce: {nonce}");
#else
                // For Android (and other non-iOS targets), use Firebase's mobile handler with platform-specific callback parameters.
                var projectId = FirebaseConfig.ProjectId;
                var apiKey = FIREBASE_API_KEY;
                
                if (string.IsNullOrWhiteSpace(projectId))
                {
                    return (false, null, "Firebase configuration is missing. Please check your FirebaseConfig.cs file.");
                }
                
                var cleanProjectId = projectId.Trim().Replace(" ", "");
                var baseAuthUrl = $"https://{cleanProjectId}.firebaseapp.com";
                var firebaseAuthHandler = $"{baseAuthUrl}/__/auth/handler";
                
#if ANDROID
                var androidPackageName = GetAndroidPackageName();
                var appCallbackUrl = $"{androidPackageName}://oauth2redirect";
                
                System.Diagnostics.Debug.WriteLine("Firebase Mobile OAuth Configuration (Android):");
                System.Diagnostics.Debug.WriteLine($"  Firebase Auth Handler: {firebaseAuthHandler}");
                System.Diagnostics.Debug.WriteLine($"  App Callback Scheme: {appCallbackUrl}");
                System.Diagnostics.Debug.WriteLine($"  Android Package: {androidPackageName}");
                Console.WriteLine("Firebase Mobile OAuth Configuration (Android):");
                Console.WriteLine($"  Firebase Auth Handler: {firebaseAuthHandler}");
                Console.WriteLine($"  App Callback Scheme: {appCallbackUrl}");
                Console.WriteLine($"  Android Package: {androidPackageName}");
                
                var firebaseMobileOAuthUrl = $"{firebaseAuthHandler}?" +
                    $"apiKey={Uri.EscapeDataString(apiKey)}&" +
                    $"appName=%5BDEFAULT%5D&" +
                    $"authType=signInViaRedirect&" +
                    $"providerId=google.com&" +
                    $"redirectUrl={Uri.EscapeDataString(firebaseAuthHandler)}&" +
                    $"continueUrl={Uri.EscapeDataString(appCallbackUrl)}&" +
                    $"apn={Uri.EscapeDataString(androidPackageName)}&" +
                    $"v=9.23.0";

                authUrl = firebaseMobileOAuthUrl;
                callbackUrl = appCallbackUrl;
#else
                // Default fallback for other platforms (e.g., Windows, MacCatalyst) can continue using the existing scheme.
                var appCallbackUrl = "com.pinebelttrophy.photojobapp2025://";
                var bundleId = "com.pinebelttrophy.photojobapp2025";
                
                System.Diagnostics.Debug.WriteLine("Firebase Mobile OAuth Configuration (default):");
                System.Diagnostics.Debug.WriteLine($"  Firebase Auth Handler: {firebaseAuthHandler}");
                System.Diagnostics.Debug.WriteLine($"  App Callback Scheme: {appCallbackUrl}");
                System.Diagnostics.Debug.WriteLine($"  Bundle ID: {bundleId}");
                Console.WriteLine("Firebase Mobile OAuth Configuration (default):");
                Console.WriteLine($"  Firebase Auth Handler: {firebaseAuthHandler}");
                Console.WriteLine($"  App Callback Scheme: {appCallbackUrl}");
                Console.WriteLine($"  Bundle ID: {bundleId}");
                
                var firebaseMobileOAuthUrl = $"{firebaseAuthHandler}?" +
                    $"apiKey={Uri.EscapeDataString(apiKey)}&" +
                    $"appName=%5BDEFAULT%5D&" +
                    $"authType=signInViaRedirect&" +
                    $"providerId=google.com&" +
                    $"redirectUrl={Uri.EscapeDataString(firebaseAuthHandler)}&" +
                    $"continueUrl={Uri.EscapeDataString(appCallbackUrl)}&" +
                    $"ibi={Uri.EscapeDataString(bundleId)}&" +
                    $"v=9.23.0";

                authUrl = firebaseMobileOAuthUrl;
                callbackUrl = appCallbackUrl;
#endif

                System.Diagnostics.Debug.WriteLine($"⚠️ USING FIREBASE MOBILE OAUTH (base domain: {baseAuthUrl})");
                Console.WriteLine($"⚠️ USING FIREBASE MOBILE OAUTH (base domain: {baseAuthUrl})");
#endif
                
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Calling WebAuthenticator with:");
                    System.Diagnostics.Debug.WriteLine($"  URL: {authUrl}");
                    System.Diagnostics.Debug.WriteLine($"  CallbackUrl: {callbackUrl}");
                    Console.WriteLine($"Calling WebAuthenticator with:");
                    Console.WriteLine($"  URL: {authUrl}");
                    Console.WriteLine($"  CallbackUrl: {callbackUrl}");
                    
                    // Use WebAuthenticator to handle the OAuth flow.
                    // On iOS we talk directly to Google; on other platforms we fall back to Firebase's handler.
                    // The platform-specific callback (custom URL scheme) will ultimately be routed through AppDelegate.OpenUrl.
                    System.Diagnostics.Debug.WriteLine("About to call WebAuthenticator.AuthenticateAsync...");
                    Console.WriteLine("About to call WebAuthenticator.AuthenticateAsync...");
                    
                    var authResult = await Microsoft.Maui.Authentication.WebAuthenticator.AuthenticateAsync(
                        new Microsoft.Maui.Authentication.WebAuthenticatorOptions
                        {
                            Url = new Uri(authUrl),
                            CallbackUrl = new Uri(callbackUrl)
                        });
                    
                    System.Diagnostics.Debug.WriteLine($"✓ OAuth callback received with {authResult.Properties.Count} properties");
                    Console.WriteLine($"✓ OAuth callback received with {authResult.Properties.Count} properties");
                    
                    // Log all properties for debugging
                    System.Diagnostics.Debug.WriteLine("OAuth callback properties:");
                    foreach (var prop in authResult.Properties)
                    {
                        System.Diagnostics.Debug.WriteLine($"  {prop.Key} = {prop.Value}");
                    }
                    
                    // The OAuth callback can return different parameters (direct Google flow or Firebase handler):
                    // 1. id_token - directly from the provider (best case)
                    // 2. code - authorization code that needs to be exchanged
                    // 3. link - deep link with token data
                    
                    // Check for id_token first (Google/Firebase might return it directly)
                    string? idToken = null;
                    if (authResult.Properties.TryGetValue("id_token", out var token1))
                        idToken = token1;
                    else if (authResult.Properties.TryGetValue("idToken", out var token2))
                        idToken = token2;
                    else if (authResult.Properties.TryGetValue("access_token", out var token3))
                        idToken = token3;
                    
                    // Look for JWT-like tokens (they start with "ey" and contain dots)
                    if (string.IsNullOrEmpty(idToken))
                    {
                        foreach (var prop in authResult.Properties)
                        {
                            if (prop.Value.Length > 100 && prop.Value.StartsWith("ey") && prop.Value.Contains("."))
                            {
                                idToken = prop.Value;
                                System.Diagnostics.Debug.WriteLine($"  Found ID token in property: {prop.Key}");
                                break;
                            }
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(idToken))
                    {
                        System.Diagnostics.Debug.WriteLine("✓ Received ID token from OAuth provider, signing in...");
                        Console.WriteLine("✓ Received ID token from OAuth provider, signing in...");
                        return await SignInWithGoogleIdTokenAsync(idToken);
                    }
                    
                    // Check for authorization code (if provider uses code flow)
                    if (authResult.Properties.TryGetValue("code", out var authCode) && !string.IsNullOrEmpty(authCode))
                    {
                        System.Diagnostics.Debug.WriteLine($"✓ Received authorization code from OAuth provider");
                        Console.WriteLine($"✓ Received authorization code from OAuth provider");
                        
                        // Depending on the provider, this might be a Firebase session code or a Google OAuth code.
                        // Check if there's a link or other parameter
                        if (authResult.Properties.TryGetValue("link", out var link) || 
                            authResult.Properties.TryGetValue("deepLink", out link))
                        {
                            System.Diagnostics.Debug.WriteLine($"Found deep link: {link}");
                            Console.WriteLine($"Found deep link in callback");
                            // Parse the deep link for tokens
                            if (link.Contains("id_token=") || link.Contains("idToken="))
                            {
                                // Extract token from link
                                var linkUri = new Uri(link);
                                var query = linkUri.Query.TrimStart('?');
                                foreach (var param in query.Split('&'))
                                {
                                    var parts = param.Split('=');
                                    if (parts.Length == 2 && (parts[0] == "id_token" || parts[0] == "idToken"))
                                    {
                                        idToken = Uri.UnescapeDataString(parts[1]);
                                        break;
                                    }
                                }
                                
                                if (!string.IsNullOrEmpty(idToken))
                                {
                                    return await SignInWithGoogleIdTokenAsync(idToken);
                                }
                            }
                        }
                        
                        return (false, null, "Received authorization code but cannot complete the exchange automatically yet. Please try the sign-in again.");
                    }
                    
                    // No token or code found
                    System.Diagnostics.Debug.WriteLine("⚠️ No token or code found in OAuth callback");
                    Console.WriteLine("⚠️ No token or code found in OAuth callback");
                    
                    return (false, null, "Google Sign In completed but no authentication data was returned. Please try again.");
                }
                catch (TaskCanceledException tce)
                {
                    // TaskCanceledException can also occur if the URL callback isn't handled properly
                    System.Diagnostics.Debug.WriteLine($"Google Sign In TaskCanceledException: {tce.Message}");
                    System.Diagnostics.Debug.WriteLine($"InnerException: {tce.InnerException?.Message}");
                    System.Diagnostics.Debug.WriteLine($"This might indicate URL callback not handled. Check:\n1. Info.plist has CFBundleURLTypes configured\n2. iOS app is registered in Firebase with bundle ID\n3. Redirect URI is authorized in Google Cloud Console");
                    
                    return (false, null, "Google Sign In failed. This usually means the callback URL isn't being handled.\n\nPlease verify:\n1. Your iOS app is registered in Firebase Console (Settings > Your apps)\n2. Bundle ID matches: com.pinebelttrophy.photojobapp2025\n3. Google Sign In is enabled in Firebase Authentication\n4. Redirect URI is authorized in Google Cloud Console");
                }
                catch (Exception webAuthEx)
                {
                    System.Diagnostics.Debug.WriteLine($"WebAuthenticator error: {webAuthEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"Exception type: {webAuthEx.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {webAuthEx.InnerException?.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {webAuthEx.StackTrace}");
                    
                    // Check if it's a Firebase configuration error
                    var errorMsg = webAuthEx.Message;
                    if (errorMsg.Contains("invalid") || errorMsg.Contains("Invalid"))
                    {
                        return (false, null, "Google Sign In configuration error. Please verify:\n1. Google Sign In is enabled in Firebase Console > Authentication > Sign-in method\n2. Your Firebase project settings are correct\n3. The redirect URL matches your bundle ID: com.pinebelttrophy.photojobapp2025\n4. Your iOS app is registered in Firebase Console");
                    }
                    else if (errorMsg.Contains("not enabled") || errorMsg.Contains("OPERATION_NOT_ALLOWED"))
                    {
                        return (false, null, "Google Sign In is not enabled in Firebase Console. Please enable it in Authentication > Sign-in method.");
                    }
                    
                    return (false, null, $"Google Sign In failed: {errorMsg}\n\nPlease ensure:\n1. Google Sign In is enabled in Firebase Console\n2. iOS app is registered in Firebase\n3. Bundle ID matches: com.pinebelttrophy.photojobapp2025");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Firebase SignInWithGoogle Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return (false, null, $"Google sign in failed: {ex.Message}");
            }
        }

        public async Task<(bool success, FirebaseUser? user, string? error)> SignInWithGoogleIdTokenAsync(string idToken)
        {
            try
            {
                var requestData = new
                {
                    postBody = $"id_token={Uri.EscapeDataString(idToken)}&providerId=google.com",
                    requestUri = "com.pinebelttrophy.photojobapp2025://",
                    returnIdpCredential = true,
                    returnSecureToken = true
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{FIREBASE_AUTH_URL}:signInWithIdp?key={FIREBASE_API_KEY}";
                System.Diagnostics.Debug.WriteLine($"Firebase SignInWithIdp URL: {url}");
                System.Diagnostics.Debug.WriteLine($"Firebase SignInWithIdp Request: {json}");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"Firebase SignInWithIdp Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Firebase SignInWithIdp Response: {responseContent}");

                var authResponse = JsonSerializer.Deserialize<FirebaseAuthResponse>(responseContent);

                if (authResponse?.Error != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Firebase SignInWithIdp Error: {authResponse.Error.Message}");
                    
                    // Provide helpful error message for common issues
                    var errorMsg = authResponse.Error.Message ?? "Unknown error";
                    if (errorMsg.Contains("OPERATION_NOT_ALLOWED") || errorMsg.Contains("not enabled"))
                    {
                        return (false, null, "Google Sign In is not enabled in Firebase Console. Please enable it in Authentication > Sign-in method.");
                    }
                    
                    return (false, null, GetErrorMessage(errorMsg));
                }

                if (authResponse?.IdToken != null && authResponse?.LocalId != null)
                {
                    var user = new FirebaseUser
                    {
                        Id = authResponse.LocalId,
                        Email = authResponse.Email ?? string.Empty,
                        DisplayName = authResponse.DisplayName ?? string.Empty,
                        IdToken = authResponse.IdToken,
                        RefreshToken = authResponse.RefreshToken ?? string.Empty
                    };

                    // Store authentication data
                    await StoreAuthDataAsync(user);

                    return (true, user, null);
                }

                return (false, null, "Google sign in failed. Please try again.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Firebase SignInWithGoogleIdToken Exception: {ex.Message}");
                return (false, null, $"Google sign in failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Exchanges a Google OAuth authorization code for tokens (id_token and access_token)
        /// This is used for the authorization code flow (response_type=code)
        /// </summary>
        private async Task<(string? idToken, string? accessToken, string? error)> ExchangeAuthCodeForTokensAsync(
            string authCode, string clientId, string redirectUri)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Exchanging authorization code for tokens...");
                Console.WriteLine($"Exchanging authorization code for tokens...");
                
                // Google's token exchange endpoint
                var tokenUrl = "https://oauth2.googleapis.com/token";
                
                // Prepare the token exchange request
                var requestData = new Dictionary<string, string>
                {
                    { "code", authCode },
                    { "client_id", clientId },
                    { "redirect_uri", redirectUri },
                    { "grant_type", "authorization_code" }
                };
                
                var content = new FormUrlEncodedContent(requestData);
                
                System.Diagnostics.Debug.WriteLine($"Token exchange request:");
                System.Diagnostics.Debug.WriteLine($"  URL: {tokenUrl}");
                System.Diagnostics.Debug.WriteLine($"  Client ID: {clientId.Substring(0, Math.Min(30, clientId.Length))}...");
                System.Diagnostics.Debug.WriteLine($"  Redirect URI: {redirectUri}");
                Console.WriteLine($"Token exchange request:");
                Console.WriteLine($"  Client ID: {clientId.Substring(0, Math.Min(30, clientId.Length))}...");
                Console.WriteLine($"  Redirect URI: {redirectUri}");
                
                var response = await _httpClient.PostAsync(tokenUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                System.Diagnostics.Debug.WriteLine($"Token exchange response status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Token exchange response: {responseContent}");
                Console.WriteLine($"Token exchange response status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent);
                    
                    if (tokenResponse?.IdToken != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"✓ Successfully exchanged code for tokens");
                        Console.WriteLine($"✓ Successfully exchanged code for tokens");
                        return (tokenResponse.IdToken, tokenResponse.AccessToken, null);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Token response missing id_token");
                        Console.WriteLine($"⚠️ Token response missing id_token");
                        return (null, null, "Token response missing id_token");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Token exchange failed: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"Response: {responseContent}");
                    Console.WriteLine($"❌ Token exchange failed: {response.StatusCode}");
                    return (null, null, $"Token exchange failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception during token exchange: {ex.Message}");
                Console.WriteLine($"Exception during token exchange: {ex.Message}");
                return (null, null, $"Token exchange error: {ex.Message}");
            }
        }
        
        public class GoogleTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }
            
            [JsonPropertyName("id_token")]
            public string? IdToken { get; set; }
            
            [JsonPropertyName("refresh_token")]
            public string? RefreshToken { get; set; }
            
            [JsonPropertyName("token_type")]
            public string? TokenType { get; set; }
            
            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
        }

        private string GetGoogleWebClientId()
        {
            // Get Web Client ID from configuration
            // This can be found in Firebase Console > Project Settings > Your apps > Web app
            // Or in Google Cloud Console > APIs & Services > Credentials
            try
            {
                return _configuration?["Firebase:GoogleWebClientId"] ?? 
                       "1021759232753-glcofi4hpt6i0jjis09t7te3lu1enk6f.apps.googleusercontent.com";
            }
            catch
            {
                // Fallback to the hardcoded client ID provided by user
                return "1021759232753-glcofi4hpt6i0jjis09t7te3lu1enk6f.apps.googleusercontent.com";
            }
        }
        
        private string GetGoogleiOSClientId()
        {
            var configuredValue = _configuration?["Firebase:GoogleiOSClientId"];
            if (!string.IsNullOrEmpty(configuredValue))
            {
                return configuredValue;
            }

#if IOS
            try
            {
                var plistPath = Foundation.NSBundle.MainBundle.PathForResource("GoogleService-Info", "plist");
                if (!string.IsNullOrEmpty(plistPath) && Foundation.NSFileManager.DefaultManager.FileExists(plistPath))
                {
                    var plist = Foundation.NSDictionary.FromFile(plistPath);
                    if (plist != null && plist.ContainsKey(new Foundation.NSString("CLIENT_ID")))
                    {
                        var plistClientId = plist["CLIENT_ID"]?.ToString();
                        if (!string.IsNullOrEmpty(plistClientId))
                        {
                            System.Diagnostics.Debug.WriteLine($"GetGoogleiOSClientId: Using CLIENT_ID from GoogleService-Info.plist: {plistClientId}");
                            Console.WriteLine($"GetGoogleiOSClientId: Using CLIENT_ID from GoogleService-Info.plist: {plistClientId}");
                            return plistClientId;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetGoogleiOSClientId: Failed to read GoogleService-Info.plist: {ex.Message}");
                Console.WriteLine($"GetGoogleiOSClientId: Failed to read GoogleService-Info.plist: {ex.Message}");
            }
#endif

            System.Diagnostics.Debug.WriteLine("GetGoogleiOSClientId: Falling back to hardcoded iOS Client ID");
            Console.WriteLine("GetGoogleiOSClientId: Falling back to hardcoded iOS Client ID");
            return "1021759232753-mm6ns7f4r20aohg8ric4med68kqtul9e.apps.googleusercontent.com";
        }

        private string? GetGoogleReversedClientId()
        {
            var configuredValue = _configuration?["Firebase:GoogleReversedClientId"];
            if (!string.IsNullOrEmpty(configuredValue))
            {
                return configuredValue;
            }

#if IOS
            try
            {
                var plistPath = Foundation.NSBundle.MainBundle.PathForResource("GoogleService-Info", "plist");
                if (!string.IsNullOrEmpty(plistPath) && Foundation.NSFileManager.DefaultManager.FileExists(plistPath))
                {
                    var plist = Foundation.NSDictionary.FromFile(plistPath);
                    if (plist != null)
                    {
                        if (plist.ContainsKey(new Foundation.NSString("REVERSED_CLIENT_ID")))
                        {
                            var reversed = plist["REVERSED_CLIENT_ID"]?.ToString();
                            if (!string.IsNullOrEmpty(reversed))
                            {
                                System.Diagnostics.Debug.WriteLine($"GetGoogleReversedClientId: Using REVERSED_CLIENT_ID from GoogleService-Info.plist: {reversed}");
                                Console.WriteLine($"GetGoogleReversedClientId: Using REVERSED_CLIENT_ID from GoogleService-Info.plist: {reversed}");
                                return reversed;
                            }
                        }

                        if (plist.ContainsKey(new Foundation.NSString("CLIENT_ID")))
                        {
                            var clientId = plist["CLIENT_ID"]?.ToString();
                            var derived = DeriveReversedClientId(clientId);
                            if (!string.IsNullOrEmpty(derived))
                            {
                                System.Diagnostics.Debug.WriteLine($"GetGoogleReversedClientId: Derived reversed ID from CLIENT_ID: {derived}");
                                Console.WriteLine($"GetGoogleReversedClientId: Derived reversed ID from CLIENT_ID: {derived}");
                                return derived;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetGoogleReversedClientId: Failed to read GoogleService-Info.plist: {ex.Message}");
                Console.WriteLine($"GetGoogleReversedClientId: Failed to read GoogleService-Info.plist: {ex.Message}");
            }
#endif

            return DeriveReversedClientId(GetGoogleiOSClientId());
        }

        private static string? DeriveReversedClientId(string? clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return null;
            }

            const string suffix = ".apps.googleusercontent.com";
            if (clientId.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                var trimmed = clientId[..^suffix.Length];
                return $"com.googleusercontent.apps.{trimmed}";
            }

            return null;
        }
        
        private string GetGoogleClientId()
        {
            // For iOS OAuth, we need to use the iOS Client ID (not Web Client ID)
            // Web clients don't support custom URL schemes
            #if IOS
            return GetGoogleiOSClientId();
            #else
            return GetGoogleWebClientId();
            #endif
        }
    }
} 