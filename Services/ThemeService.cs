using Microsoft.Maui.Graphics;
using PhotoJobApp.Models;

namespace PhotoJobApp.Services
{
    public class ThemeService
    {
        private static ThemeService? _instance;
        private static readonly object _lock = new object();

        public static ThemeService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new ThemeService();
                    }
                }
                return _instance;
            }
        }

        private ThemeService() { }

        public Color PrimaryColor
        {
            get
            {
                var colorString = Preferences.Get("PrimaryColor", "#512BD4");
                return Color.FromArgb(colorString);
            }
            set
            {
                Preferences.Set("PrimaryColor", value.ToHex());
            }
        }

        public Color SecondaryColor
        {
            get
            {
                var colorString = Preferences.Get("SecondaryColor", "#4ECDC4");
                return Color.FromArgb(colorString);
            }
            set
            {
                Preferences.Set("SecondaryColor", value.ToHex());
            }
        }

        public Color AccentColor
        {
            get
            {
                var colorString = Preferences.Get("AccentColor", "#FF6B6B");
                return Color.FromArgb(colorString);
            }
            set
            {
                Preferences.Set("AccentColor", value.ToHex());
            }
        }

        public Color BackgroundColor
        {
            get
            {
                var colorString = Preferences.Get("BackgroundColor", "#FFFFFF");
                return Color.FromArgb(colorString);
            }
            set
            {
                Preferences.Set("BackgroundColor", value.ToHex());
            }
        }

        public Color SurfaceColor
        {
            get
            {
                var colorString = Preferences.Get("SurfaceColor", "#F8F9FA");
                return Color.FromArgb(colorString);
            }
            set
            {
                Preferences.Set("SurfaceColor", value.ToHex());
            }
        }

        public void ApplyThemeToPage(ContentPage page)
        {
            if (page == null) return;

            // Apply background color to the page
            page.BackgroundColor = BackgroundColor;

            // Apply theme to all child elements
            ApplyThemeToElement(page.Content);
        }

        public void ApplyThemeToPage(ContentPage page, Color customBackgroundColor)
        {
            if (page == null) return;

            // Apply custom background color to the page
            page.BackgroundColor = customBackgroundColor;

            // Apply theme to all child elements
            ApplyThemeToElement(page.Content);
        }

        private void ApplyThemeToElement(IView? element)
        {
            if (element == null) return;

            // Apply theme based on element type
            if (element is Frame frame)
            {
                frame.BackgroundColor = SurfaceColor;
            }
            else if (element is Button button)
            {
                // Only apply if button doesn't have a specific color set
                if (button.BackgroundColor == null)
                {
                    button.BackgroundColor = PrimaryColor;
                }
            }
            else if (element is Label label)
            {
                // Apply text color for certain labels
                if (label.TextColor == null)
                {
                    label.TextColor = PrimaryColor;
                }
            }

            // Recursively apply to child elements
            if (element is Layout layout)
            {
                foreach (var child in layout.Children)
                {
                    ApplyThemeToElement(child);
                }
            }
        }

        public void UpdateThemeFromJobType(JobType jobType)
        {
            if (jobType == null) return;

            try
            {
                // Update primary color from job type
                if (!string.IsNullOrEmpty(jobType.Color))
                {
                    PrimaryColor = Color.FromArgb(jobType.Color);
                    // Also set background color to a darker version of the primary color
                    BackgroundColor = GetLighterColor(Color.FromArgb(jobType.Color), 0.7f);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating theme from job type: {ex.Message}");
            }
        }

        public Color GetLighterColor(Color baseColor, float factor)
        {
            return new Color(
                Math.Min(1.0f, baseColor.Red + (1.0f - baseColor.Red) * factor),
                Math.Min(1.0f, baseColor.Green + (1.0f - baseColor.Green) * factor),
                Math.Min(1.0f, baseColor.Blue + (1.0f - baseColor.Blue) * factor),
                baseColor.Alpha
            );
        }

        public static List<ColorOption> GetAvailableColors()
        {
            return new List<ColorOption>
            {
                new ColorOption { Name = "Purple", HexCode = "#512BD4" },
                new ColorOption { Name = "Blue", HexCode = "#2196F3" },
                new ColorOption { Name = "Teal", HexCode = "#4ECDC4" },
                new ColorOption { Name = "Green", HexCode = "#4CAF50" },
                new ColorOption { Name = "Lime", HexCode = "#96CEB4" },
                new ColorOption { Name = "Yellow", HexCode = "#FFEAA7" },
                new ColorOption { Name = "Orange", HexCode = "#FF9800" },
                new ColorOption { Name = "Red", HexCode = "#FF6B6B" },
                new ColorOption { Name = "Pink", HexCode = "#E91E63" },
                new ColorOption { Name = "Magenta", HexCode = "#DDA0DD" },
                new ColorOption { Name = "Indigo", HexCode = "#3F51B5" },
                new ColorOption { Name = "Cyan", HexCode = "#00BCD4" },
                new ColorOption { Name = "Emerald", HexCode = "#009688" },
                new ColorOption { Name = "Amber", HexCode = "#FFC107" },
                new ColorOption { Name = "Deep Orange", HexCode = "#FF5722" },
                new ColorOption { Name = "Light Blue", HexCode = "#03A9F4" },
                new ColorOption { Name = "Light Green", HexCode = "#8BC34A" },
                new ColorOption { Name = "Deep Purple", HexCode = "#673AB7" },
                new ColorOption { Name = "Brown", HexCode = "#795548" },
                new ColorOption { Name = "Gray", HexCode = "#9E9E9E" }
            };
        }

        public static string GetColorName(string hexCode)
        {
            var colors = GetAvailableColors();
            var color = colors.FirstOrDefault(c => c.HexCode.Equals(hexCode, StringComparison.OrdinalIgnoreCase));
            return color?.Name ?? "Custom";
        }
    }

    public class ColorOption
    {
        public string Name { get; set; } = string.Empty;
        public string HexCode { get; set; } = string.Empty;
    }
} 