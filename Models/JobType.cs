using SQLite;

namespace PhotoJobApp.Models
{
    [Table("JobTypes")]
    public class JobType
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool HasPhotos { get; set; } = false;

        public bool HasLocation { get; set; } = false;

        public bool HasClientInfo { get; set; } = true;

        public bool HasPricing { get; set; } = true;

        public bool HasDueDate { get; set; } = true;

        public bool HasStatus { get; set; } = true;

        public bool HasNotes { get; set; } = true;

        public bool HasUrgentFlag { get; set; } = true;

        public string Color { get; set; } = "#512BD4";

        public string Icon { get; set; } = "📷";

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string UserId { get; set; } = string.Empty;

        // Custom fields for this job type
        public string CustomFields { get; set; } = string.Empty; // JSON string

        // Status options for this job type
        public string StatusOptions { get; set; } = "Pending,In Progress,Completed,Cancelled"; // Comma-separated

        // Computed properties
        [Ignore]
        public string FormattedCreatedDate => CreatedDate.ToString("MMM dd, yyyy");

        [Ignore]
        public List<string> StatusList => StatusOptions.Split(',').Select(s => s.Trim()).ToList();

        [Ignore]
        public List<CustomField> CustomFieldsList
        {
            get
            {
                if (string.IsNullOrEmpty(CustomFields))
                    return new List<CustomField>();
                
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<CustomField>>(CustomFields) ?? new List<CustomField>();
                }
                catch
                {
                    return new List<CustomField>();
                }
            }
            set
            {
                CustomFields = System.Text.Json.JsonSerializer.Serialize(value);
            }
        }
    }

    public class CustomField
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "text"; // text, number, date, boolean, dropdown
        public bool Required { get; set; } = false;
        public string Options { get; set; } = string.Empty; // For dropdown type
        public string DefaultValue { get; set; } = string.Empty;
    }
} 