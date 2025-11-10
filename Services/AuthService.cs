using System.Text.Json;
using Microsoft.Maui.Storage;

namespace PhotoJobApp.Services
{
    public class AuthService
    {
        private const string UserKey = "CurrentUser";
        private const string AuthKey = "IsAuthenticated";
        private const string SecureUserKey = "AuthServiceUser:v1";

        public class User
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public DateTime CreatedDate { get; set; } = DateTime.Now;
        }

        public async Task<bool> SignInAsync(string email, string password)
        {
            // Basic validation for production app
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return false;

            // Validate email format
            if (!email.Contains("@"))
                return false;

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                Name = email.Split('@')[0],
                CreatedDate = DateTime.Now
            };

            await PersistUserAsync(user);

            return true;
        }

        public async Task<bool> SignUpAsync(string email, string password, string name)
        {
            // Basic registration validation
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(name))
                return false;

            if (!email.Contains("@"))
                return false;

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                Name = name,
                CreatedDate = DateTime.Now
            };

            await PersistUserAsync(user);

            return true;
        }

        public void SignOut()
        {
            Preferences.Remove(UserKey);
            Preferences.Set(AuthKey, false);

            try
            {
                SecureStorage.Remove(SecureUserKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SecureStorage.Remove failed: {ex.Message}");
                Console.WriteLine($"SecureStorage.Remove failed: {ex.Message}");
            }
        }

        public User? GetCurrentUser()
        {
            string userJson = string.Empty;

            try
            {
                var secureJson = SecureStorage.GetAsync(SecureUserKey).GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(secureJson))
                {
                    userJson = secureJson;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SecureStorage.GetAsync failed: {ex.Message}");
                Console.WriteLine($"SecureStorage.GetAsync failed: {ex.Message}");
            }

            if (string.IsNullOrEmpty(userJson))
            {
                userJson = Preferences.Get(UserKey, "");
            }

            if (string.IsNullOrEmpty(userJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<User>(userJson);
            }
            catch
            {
                return null;
            }
        }

        public bool IsAuthenticated()
        {
            if (Preferences.Get(AuthKey, false))
                return true;

            try
            {
                var secureJson = SecureStorage.GetAsync(SecureUserKey).GetAwaiter().GetResult();
                return !string.IsNullOrEmpty(secureJson);
            }
            catch
            {
                return false;
            }
        }

        private async Task PersistUserAsync(User user)
        {
            var userJson = JsonSerializer.Serialize(user);
            Preferences.Set(UserKey, userJson);
            Preferences.Set(AuthKey, true);

            try
            {
                await SecureStorage.SetAsync(SecureUserKey, userJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SecureStorage.SetAsync failed: {ex.Message}");
                Console.WriteLine($"SecureStorage.SetAsync failed: {ex.Message}");
            }
        }
    }
} 